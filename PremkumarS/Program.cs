using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;

namespace ThreadedFileWriter
{
    internal static class Program
    {
        // Default path: /log/out.txt.
        // For local Windows runs C:\junk\out.txt.
        private const string DefaultOutputPath = "/log/out.txt";
        private static readonly ConcurrentQueue<Exception> ThreadExceptions = new();

        static int Main(string[] args)
        {
            string outputPath = args.Length > 0
                ? args[0]
                : (Environment.GetEnvironmentVariable("LOG_PATH") ?? DefaultOutputPath);

            // local windows run
            if (outputPath == DefaultOutputPath && OperatingSystem.IsWindows())
            {
                var filePath = Path.Combine(@"C:\junk", "out.txt");
                Console.WriteLine($"[INFO] Windows local run detected. Using '{filePath}'.");
                outputPath = filePath;
            }

            // unhandled exception logging
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                Console.Error.WriteLine($"[FATAL] UnhandledException: {e.ExceptionObject}");
            };

            try
            {
                // Directory exists
                string? dir = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrWhiteSpace(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                using var appender = new FileAppender(outputPath);

                // Initialize file
                appender.InitializeWithHeader();

                const int threadCount = 10;
                const int writesPerThread = 10;
                Thread[] threads = new Thread[threadCount];

                //Ten threads each write ten lines
                for (int i = 0; i < threadCount; i++)
                {
                    var worker = new LineWriterWorker(appender, writesPerThread, ThreadExceptions);
                    threads[i] = new Thread(worker.Run)
                    {
                        IsBackground = false,
                        Name = $"Writer-{i + 1}"
                    };
                }

                foreach (var t in threads) t.Start();
                foreach (var t in threads) t.Join();

                if (!ThreadExceptions.IsEmpty)
                {
                    Console.Error.WriteLine("One or more background threads errors:");
                    foreach (var ex in ThreadExceptions)
                    {
                        Console.Error.WriteLine(ex.ToString());
                    }
                    Console.WriteLine("Press Enter to exit...");
                    Console.ReadLine();
                    return 1;
                }

                Console.WriteLine($"All threads done. output written to {outputPath}");
                Console.Write("Press enter to exit...");
                Console.ReadLine();
                return 0;
            }
            catch (UnauthorizedAccessException uae)
            {
                Console.Error.WriteLine($"[ERROR] Access denied for '{outputPath}': {uae.Message}");
                Console.WriteLine("Press Enter to exit...");
                Console.ReadLine();
                return 2;
            }
            catch (DirectoryNotFoundException dnfe)
            {
                Console.Error.WriteLine($"[ERROR] Directory not found path '{outputPath}': {dnfe.Message}");
                Console.WriteLine("Press enter to exit...");
                Console.ReadLine();
                return 3;
            }
            catch (IOException ioe)
            {
                Console.Error.WriteLine($"[ERROR] I/O error while writing '{outputPath}': {ioe.Message}");
                Console.WriteLine("Press Enter to exit...");
                Console.ReadLine();
                return 4;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ERROR] Unexpected error: {ex}");
                Console.WriteLine("Press Enter to exit...");
                Console.ReadLine();
                return 5;
            }
        }
    }
}