using ThreadProgram.Core;
using ThreadProgram.Infrastructure;
using ThreadProgram.Services;

class Program
{
    static async Task Main()
    {
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            Console.WriteLine($"[FATAL] {e.ExceptionObject}");
        };

        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            Console.WriteLine($"[TASK ERROR] {e.Exception}");
            e.SetObserved();
        };

        ILogger logger = new Logger();

        try
        {
            using var fileLock = new FileLock("shared.txt");
            var worker = new FileWorker(fileLock, logger);

            using var cts = new CancellationTokenSource();
            var task = worker.RunAsync(cts.Token);

            Console.WriteLine("Press ENTER to stop...");
            Console.ReadLine();

            cts.Cancel();
            await task;
        }
        catch (Exception ex)
        {
            logger.LogError(ex);
        }
    }
}