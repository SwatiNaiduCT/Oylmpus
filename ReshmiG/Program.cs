using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using MultiThreadedFileWriter.Infrastructure;

// Multi-threaded file writer demo
// Writes initial line "0, 0, HH:MM:SS.mmm" then launches 10 threads
// Each thread appends 10 lines of the form "<line_count>, <thread_id>, <timestamp>"

var logDir = Environment.GetEnvironmentVariable("LOG_DIR")
             ?? (OperatingSystem.IsWindows() ? "C:\\log" : "/log");
var LogPath = Path.Combine(logDir, "out.txt");

try
{
    using IFileLogger logger = new FileLogger(LogPath);

    logger.InitializeFirstLine();

    long counter = 0;

    var exceptions = new ConcurrentBag<Exception>();

    var tasks = Enumerable.Range(0, 10).Select(_ => Task.Run(() =>
    {
        try
        {
            for (int i = 0; i < 10; i++)
            {
                var lineCount = Interlocked.Increment(ref counter); // 1..100
                var threadId = Thread.CurrentThread.ManagedThreadId;
                var ts = DateTime.Now.ToString("HH:mm:ss.fff");

                logger.AppendLine(lineCount, threadId, ts);
            }
        }
        catch (Exception ex)
        {
            exceptions.Add(ex);
        }
    })).ToArray();

    Task.WaitAll(tasks);

    if (!exceptions.IsEmpty)
    {
        Console.WriteLine("One or more background threads threw exceptions:");
        foreach (var ex in exceptions)
        {
            Console.WriteLine(ex.ToString());
        }

        try
        {
            var errPath = Path.Combine(Path.GetDirectoryName(LogPath) ?? "/log", "error.log");
            File.AppendAllLines(errPath, exceptions.Select(e => e.ToString()));
        }
        catch
        {
            // Swallow: we already reported to console
        }
    }

    Console.WriteLine("All threads completed.");

    var waitForKey = string.Equals(Environment.GetEnvironmentVariable("WAIT_FOR_KEY"), "true", StringComparison.OrdinalIgnoreCase);
    if (waitForKey)
    {
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey(intercept: true);
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Fatal error: {ex.Message}");
    Console.Error.WriteLine(ex.ToString());
    Environment.ExitCode = 1;
}