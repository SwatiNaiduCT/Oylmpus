
using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ThreadWriter
{
    /// <summary>
    /// Strongly typed class for capturing per-thread failures (record replaced).
    /// </summary>
    public sealed class ThreadFailure
    {
        public int ThreadId { get; private set; }
        public Exception Exception { get; private set; }

        public ThreadFailure(int threadId, Exception exception)
        {
            if (exception == null) throw new ArgumentNullException(nameof(exception));
            ThreadId = threadId;
            Exception = exception;
        }
    }

    /// <summary>
    /// Provides high-precision timestamp strings in HH:mm:ss.fff format.
    /// </summary>
    public static class Timestamp
    {
        public static string Now()
        {
            return DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);
        }
    }

    /// <summary>
    /// Thread-safe line counter. Ensures global monotonic increments across threads.
    /// </summary>
    public sealed class LineCounter
    {
        private int _current;

        public LineCounter(int initial)
        {
            _current = initial;
        }

        public int Next()
        {
            return Interlocked.Increment(ref _current);
        }

        public int Current
        {
            get { return Volatile.Read(ref _current); }
        }
    }

    /// <summary>
    /// A simple interface for file writing.
    /// </summary>
    public interface IFileSink : IDisposable
    {
        /// <summary>Creates or truncates the file and writes the initial header line: "0, 0, timestamp".</summary>
        void Initialize();

        /// <summary>Opens the file for append. Must be called after Initialize.</summary>
        void OpenAppend();

        /// <summary>
        /// Writes a single line with synchronized file access.
        /// NOTE: CHANGED — line increment happens inside the sink to guarantee strict 1..100 ordering.
        /// </summary>
        void AppendLine(int threadId, string timestamp);
    }

    /// <summary>
    /// File sink with synchronized access using an internal lock and a shared StreamWriter.
    /// </summary>
    public sealed class SafeFileSink : IFileSink
    {
        private readonly string _filePath;
        private readonly object _fileLock = new object();

        // CHANGED: Hold the LineCounter here so we can increment inside the critical section.
        private readonly LineCounter _counter;

        private StreamWriter _writer;

        // CHANGED: Accept LineCounter in constructor.
        public SafeFileSink(string filePath, LineCounter counter)
        {
            _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            _counter = counter ?? throw new ArgumentNullException(nameof(counter));
        }

        public void Initialize()
        {
            // Ensure directory exists
            string dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            // Truncate (create fresh) and write the initial line
            using (var fs = new FileStream(_filePath, FileMode.Create, FileAccess.Write, FileShare.Read))
            using (var sw = new StreamWriter(fs, new UTF8Encoding(false)))
            {
                sw.WriteLine("0, 0, " + Timestamp.Now());
                sw.Flush();
            }
        }

        public void OpenAppend()
        {
            // Use append with shared read; keep a single writer for all threads
            var fs = new FileStream(_filePath, FileMode.Append, FileAccess.Write, FileShare.Read);
            _writer = new StreamWriter(fs, new UTF8Encoding(false));
            _writer.AutoFlush = true;
        }

        public void AppendLine(int threadId, string timestamp)
        {
            if (_writer == null)
                throw new InvalidOperationException("File sink not opened for append. Call OpenAppend() after Initialize().");

            // CHANGED: Do increment + write inside the same lock to guarantee strict ordering.
            lock (_fileLock)
            {
                int nextLine = _counter.Next();
                _writer.WriteLine(string.Format("{0}, {1}, {2}", nextLine, threadId, timestamp));
            }
        }

        public void Dispose()
        {
            lock (_fileLock)
            {
                if (_writer != null)
                {
                    _writer.Dispose();
                    _writer = null;
                }
            }
        }
    }

    /// <summary>
    /// Worker encapsulates the thread routine performing N writes to the shared file sink.
    /// </summary>
    public sealed class Worker
    {
        private readonly IFileSink _sink;
        private readonly LineCounter _counter;
        private readonly ManualResetEventSlim _startGate;
        private readonly ConcurrentBag<ThreadFailure> _failures;
        private readonly int _writes;

        public Worker(IFileSink sink, LineCounter counter, ManualResetEventSlim startGate, ConcurrentBag<ThreadFailure> failures, int writes)
        {
            _sink = sink;
            _counter = counter;
            _startGate = startGate;
            _failures = failures;
            _writes = writes;
        }

        public void Run()
        {
            // All threads wait here and start at the same instant
            _startGate.Wait();

            int tid = Thread.CurrentThread.ManagedThreadId;
            try
            {
                for (int i = 0; i < _writes; i++)
                {
                    // CHANGED: Do not get nextLine here; sink will handle increment + write atomically.
                    _sink.AppendLine(tid, Timestamp.Now());
                }
            }
            catch (Exception ex)
            {
                _failures.Add(new ThreadFailure(tid, ex));
            }
        }
    }

    public static class Program
    {
        private const string OutputPath = @"C:\log\out.txt"; // for local run
        // private const string OutputPath = "/log/out.txt"; // for Docker/Linux container

        private const int ThreadCount = 10;
        private const int WritesPerThread = 10;

        public static int Main(string[] args)
        {
            // Global exception handlers to demonstrate behavior in "UI-like" event scenarios
            AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
            {
                Console.Error.WriteLine("[FATAL] Unhandled exception: " + eventArgs.ExceptionObject);
            };

            TaskScheduler.UnobservedTaskException += (sender, eventArgs) =>
            {
                Console.Error.WriteLine("[ERROR] Unobserved task exception: " + eventArgs.Exception);
                eventArgs.SetObserved();
            };

            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                try
                {
                    Console.WriteLine("[INFO] Ctrl+C pressed. Attempting graceful shutdown...");
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("[ERROR] Exception in CancelKeyPress handler: " + ex);
                }
            };

            PrintSystemInfo();

            var failures = new ConcurrentBag<ThreadFailure>();
            var startGate = new ManualResetEventSlim(false);
            var counter = new LineCounter(0);

            try
            {
                // CHANGED: Pass the counter into the sink so it can increment within its lock.
                using (IFileSink sink = new SafeFileSink(OutputPath, counter))
                {
                    // Initialize file with first line "0, 0, timestamp"
                    sink.Initialize();

                    // Now open for append and keep it open for all threads
                    sink.OpenAppend();

                    // Create and start threads
                    var threads = new Thread[ThreadCount];
                    for (int i = 0; i < ThreadCount; i++)
                    {
                        var worker = new Worker(sink, counter, startGate, failures, WritesPerThread);
                        var thread = new Thread(new ThreadStart(worker.Run));
                        thread.IsBackground = true;
                        thread.Name = "WriterThread-" + (i + 1);
                        threads[i] = thread;
                        thread.Start();
                    }

                    // Release the start gate so all threads begin simultaneously
                    startGate.Set();

                    // Join all threads (graceful termination after 10 writes each)
                    for (int i = 0; i < threads.Length; i++)
                    {
                        threads[i].Join();
                    }
                }
            }
            catch (UnauthorizedAccessException uae)
            {
                Console.Error.WriteLine("[ERROR] Permission denied creating or writing to file '" + OutputPath + "'. " + uae.Message);
                Console.WriteLine("Press Enter to exit...");
                Console.ReadLine();
                return 1;
            }
            catch (DirectoryNotFoundException dnfe)
            {
                Console.Error.WriteLine("[ERROR] Directory for '" + OutputPath + "' not found and could not be created. " + dnfe.Message);
                Console.WriteLine("Press Enter to exit...");
                Console.ReadLine();
                return 1;
            }
            catch (IOException ioe)
            {
                Console.Error.WriteLine("[ERROR] I/O error accessing '" + OutputPath + "'. " + ioe.Message);
                Console.WriteLine("Press Enter to exit...");
                Console.ReadLine();
                return 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("[ERROR] Unexpected error: " + ex);
                Console.WriteLine("Press Enter to exit...");
                Console.ReadLine();
                return 1;
            }
            finally
            {
                startGate.Dispose();
            }

            // Report any thread exceptions
            if (!failures.IsEmpty)
            {
                Console.Error.WriteLine("[ERROR] One or more background threads encountered exceptions:");
                foreach (var f in failures)
                {
                    Console.Error.WriteLine("  Thread " + f.ThreadId + ": " + f.Exception);
                }
            }
            else
            {
                Console.WriteLine("[INFO] All threads completed successfully.");
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey(true);
            return failures.IsEmpty ? 0 : 2;
        }

        private static void PrintSystemInfo()
        {
            try
            {
                Console.WriteLine("=== System Info ===");
                Console.WriteLine("OS: " + Environment.OSVersion);
                Console.WriteLine(".NET version: " + Environment.Version);
                Console.WriteLine("CPU count: " + Environment.ProcessorCount);
                string inContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER");
                Console.WriteLine("Running in container: " + (inContainer ?? "unknown"));
                Console.WriteLine("Current directory: " + Directory.GetCurrentDirectory());
                Console.WriteLine("===================");
            }
            catch
            {
                // Non-fatal; ignore system info errors.
            }
        }
    }
}
