using Microsoft.Extensions.Configuration;
using Tasklog.Config;
using Tasklog.Infrastructure;
using Tasklog.Services;

class Program
{
    static void Main(string[] args)
    {
        try
        {
            var config = LoadConfig(args);
            ConfigurationValidator.Validate(config);

            using var writer = new ConcurrentFileWriter(config.OutputFilePath);

            var tasks = new List<Task>();

            for (int i = 0; i < config.ThreadCount; i++)
            {
                var worker = new Worker(writer, config.WritesPerThread);
                tasks.Add(Task.Run(worker.Run));
            }

            Task.WaitAll(tasks.ToArray());

            Console.WriteLine("All threads completed. Press any key to exit.");
            Console.ReadKey();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FATAL] {ex.Message}");
        }
    }

    private static AppConfig LoadConfig(string[] args)
    {
        var config = new AppConfig();

        IConfiguration configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .AddCommandLine(args)
            .Build();

        configuration.GetSection("AppConfig").Bind(config);
        return config;
    }
}