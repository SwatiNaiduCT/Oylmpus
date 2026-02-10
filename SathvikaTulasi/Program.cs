using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

namespace ThreadSafeLogger
{

    public interface ITimeProvider
    {
        DateTime UtcNow { get; }
    }

    public sealed class SystemTimeProvider : ITimeProvider
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }

    /// <summary>
    /// Represents a thread-safe log file that guarantees:
    /// - Strictly increasing line numbers across threads
    /// - Synchronized access to a single file writer
    /// - Deterministic timestamp formatting (HH:mm:ss.fff)
    /// </summary>
    public sealed class LogFile : IDisposable
    {
        private readonly StreamWriter _writer;
        private readonly object _ioLock = new object();
        private int _lineCounter;
        private readonly ITimeProvider _timeprovider;

        public string FilePath { get; }

        public LogFile(string filePath, ITimeProvider timeProvider)
        {
            FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            _timeprovider= timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
            var dir = Path.GetDirectoryName(filePath);
            if (string.IsNullOrWhiteSpace(dir))
                throw new ArgumentException("Invalid file path: directory is missing.", nameof(filePath));

            // Ensure the directory exists; this can throw (e.g., permissions).
            Directory.CreateDirectory(dir);

            // Create (overwrite) the file, allow readers to read while we write.
            var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read);
            _writer = new StreamWriter(fs, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)) { AutoFlush = true };

            // Initialize header line: 0, 0, <timestamp>
            _lineCounter = 0;
            WriteRaw(0, 0, _timeprovider);
        }

        /// <summary>
        /// Adds a line using the next global line number and the current thread ID.
        /// </summary>
        public void AppendLineForCurrentThread()
        {
            //int next = Interlocked.Increment(ref _lineCounter);
            //DateTime now = DateTime.Now;
            //int threadId = Environment.CurrentManagedThreadId;
            //WriteRaw(next, threadId, now);

            lock (_ioLock)
            {
                int next = ++_lineCounter; // increment INSIDE the same lock
                string now = _timeprovider.UtcNow.ToString("HH: mm:ss.fff", CultureInfo.InvariantCulture);
                int threadId = Environment.CurrentManagedThreadId;

                string line = $"{next}, {threadId}, {now:HH:mm:ss.fff}";
                _writer.WriteLine(line); // write under the same lock
            }

        }

        /// <summary>
        /// Low-level, synchronized write ensuring entire line is written atomically.
        /// </summary>
        private void WriteRaw(int lineCount, int threadId, ITimeProvider timestamp)
        {
            string line = $"{lineCount}, {threadId}, {timestamp.UtcNow.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture)}";
            lock (_ioLock)
            {
                _writer.WriteLine(line);
            }
        }

        public void Dispose()
        {
            lock (_ioLock)
            {
                _writer.Dispose();
            }
        }
    }

    /// <summary>
    /// Encapsulates the worker that appends N lines to the shared log and reports errors.
    /// </summary>
    public sealed class LogWorker
    {
        private readonly LogFile _logFile;
        private readonly int _writesPerThread;
        private readonly ConcurrentBag<Exception> _exceptions;

        public LogWorker(LogFile logFile, int writesPerThread, ConcurrentBag<Exception> exceptions)
        {
            _logFile = logFile ?? throw new ArgumentNullException(nameof(logFile));
            _writesPerThread = writesPerThread;
            _exceptions = exceptions ?? throw new ArgumentNullException(nameof(exceptions));
        }

        public void Run()
        {
            try
            {
                for (int i = 0; i < _writesPerThread; i++)
                {
                    _logFile.AppendLineForCurrentThread();
                }
            }
            catch (Exception ex)
            {
                // Capture thread exceptions without crashing the process
                _exceptions.Add(new Exception(
                    $"Thread {Environment.CurrentManagedThreadId} failed: {ex.Message}", ex));
            }
        }
    }

    public static class Program
    {
        private const int ThreadCount = 10;
        private const int WritesPerThread = 10;

        // A simple global flag / exit code
        private static int _exitCode = 0;

        public static int Main(string[] args)
        {
            // Global exception handlers: safety net for any unobserved/unhandled exceptions.
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                Console.Error.WriteLine($"[FATAL] Unhandled exception: {e.ExceptionObject}");
                _exitCode = 1;
            };

            // Optional: capture Task exceptions (if Tasks are ever used)
            System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                Console.Error.WriteLine($"[ERROR] Unobserved task exception: {e.Exception}");
                e.SetObserved();
                _exitCode = 1;
            };

            // Optional: make Ctrl+C graceful
            Console.CancelKeyPress += (s, e) =>
            {
                Console.WriteLine("Cancellation requested (Ctrl+C). Exiting gracefully...");
                e.Cancel = true;
                _exitCode = 130; // 128 + SIGINT
            };

            // Determine output path (default required by spec: /log/out.txt)
            // Allow override via env var LOG_DIR if desired.
            string logDir = Environment.GetEnvironmentVariable("LOG_DIR") ?? "/log";
            string filePath = Path.Combine(logDir, "out.txt");

            var threadExceptions = new ConcurrentBag<Exception>();

            try
            {
                ITimeProvider timeProvider = new SystemTimeProvider();
                using var logFile = new LogFile(filePath, timeProvider);

                Console.WriteLine($"Writing to: {logFile.FilePath}");
                Console.WriteLine($"Threads: {ThreadCount}, Writes per thread: {WritesPerThread}");

                // Start threads
                Thread[] threads = new Thread[ThreadCount];
                var worker = new LogWorker(logFile, WritesPerThread, threadExceptions);

                for (int i = 0; i < ThreadCount; i++)
                {
                    threads[i] = new Thread(worker.Run)
                    {
                        IsBackground = false, // Foreground threads—process waits for Join
                        Name = $"LogWorker-{i + 1}"
                    };
                    threads[i].Start();
                }

                // Wait for all threads to finish
                foreach (var t in threads)
                {
                    t.Join();
                }

                // Report any background errors
                if (!threadExceptions.IsEmpty)
                {
                    Console.Error.WriteLine("One or more threads encountered errors:");
                    foreach (var ex in threadExceptions)
                    {
                        Console.Error.WriteLine($" - {ex.GetType().Name}: {ex.Message}");
                    }
                    _exitCode = 2;
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.Error.WriteLine($"[ERROR] Access denied creating or writing file '{filePath}'. {ex.Message}");
                _exitCode = 3;
            }
            catch (DirectoryNotFoundException ex)
            {
                Console.Error.WriteLine($"[ERROR] Directory not found for path '{filePath}'. {ex.Message}");
                _exitCode = 4;
            }
            catch (IOException ex)
            {
                Console.Error.WriteLine($"[ERROR] I/O error writing '{filePath}'. {ex.Message}");
                _exitCode = 5;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ERROR] Unexpected error: {ex.GetType().Name}: {ex.Message}");
                _exitCode = 6;
            }

            Console.WriteLine("All threads completed.");
            Console.WriteLine("Press any key to exit...");
            // If stdin is not interactive (e.g., docker without -i), ReadKey will throw.
            try
            {
                Console.ReadKey(intercept: true);
            }
            catch
            {
                // Non-interactive container run: ignore
            }

            return _exitCode;
        }
    }
}