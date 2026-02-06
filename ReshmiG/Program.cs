using MultiThreadedFileWriter.Infrastructure;
using System.Collections.Concurrent;

// Multi-threaded file writer demo
// Writes initial line "0, 0, HH:MM:SS.mmm" then launches 10 threads
// Each thread appends 10 lines of the form "<line_count>, <thread_id>, <timestamp>"

// Load configuration from appsettings.json (optional) or environment variables.
var config = AppSettingsProvider.LoadFromFile("appsettings.json");

var logDir = config.LogDirectory ?? Environment.GetEnvironmentVariable("LOG_DIR");
if (string.IsNullOrWhiteSpace(logDir))
{
    throw new InvalidOperationException("Log directory must be provided by configuration (appsettings.json) or the LOG_DIR environment variable.");
}

var fileName = config.LogFileName ?? Environment.GetEnvironmentVariable("LOG_FILE_NAME");
if (string.IsNullOrWhiteSpace(fileName))
{
    throw new InvalidOperationException("Log file name must be provided by configuration (appsettings.json) or the LOG_FILE_NAME environment variable.");
}

var logPath = Path.Combine(logDir, fileName);

try
{
    using IFileLogger logger = new FileLogger(logPath);

    logger.InitializeFirstLine();

    long counter = 0;

    var exceptions = new ConcurrentBag<Exception>();

    var threadCount = config.ThreadCount ?? ParseIntFromEnvironment("THREAD_COUNT") ?? 10;
    var linesPerThread = config.LinesPerThread ?? ParseIntFromEnvironment("LINES_PER_THREAD") ?? 10;

    var tasks = Enumerable.Range(0, threadCount).Select(_ => Task.Run(() =>
    {
        try
        {
            for (int i = 0; i < linesPerThread; i++)
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
            var errorFileName = config.ErrorFileName ?? Environment.GetEnvironmentVariable("ERROR_FILE_NAME");
            if (string.IsNullOrWhiteSpace(errorFileName))
            {
                throw new InvalidOperationException("Error file name must be provided by configuration (appsettings.json) or the ERROR_FILE_NAME environment variable.");
            }

            var errPath = Path.Combine(logDir, errorFileName);
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

static int? ParseIntFromEnvironment(string name)
{
    var value = Environment.GetEnvironmentVariable(name);
    return int.TryParse(value, out var result) ? result : null;
}
