using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Tasklog.Config;

class Program
{
    private static readonly object _fileLock = new();
    private static int _lineCount = 0;
    private static AppConfig _config = new();

    static void Main(string[] args)
    {
        try
        {
            LoadConfiguration(args);
            InitializeFile();

            var tasks = new List<Task>();

            for (int i = 0; i < 10; i++)
            {
                tasks.Add(Task.Run(WriteLines));
            }

            // Wait for all threads to finish
            Task.WaitAll(tasks.ToArray());

            Console.WriteLine("All threads completed. Press any key to exit.");
            Console.ReadKey();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FATAL ERROR] {ex}");
        }
    }

    private static void LoadConfiguration(string[] args)
    {
        IConfiguration config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .AddCommandLine(args)
            .Build();

        config.GetSection("AppConfig").Bind(_config);
    }

    private static void InitializeFile()
    {
        var directory = Path.GetDirectoryName(_config.OutputFilePath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory!);
        }

        var timestamp = GetTimestamp();

        lock (_fileLock)
        {
            _lineCount = 0;
            File.WriteAllText(_config.OutputFilePath, $"0, 0, {timestamp}{Environment.NewLine}");
            _lineCount++;
        }
    }

    private static void WriteLines()
    {
        try
        {
            int threadId = Thread.CurrentThread.ManagedThreadId;

            for (int i = 0; i < 10; i++)
            {
                //int currentLine = Interlocked.Increment(ref _lineCount);
                string timestamp = GetTimestamp();

                string line = $"{_lineCount}, {threadId}, {timestamp}";
                _lineCount++;

                lock (_fileLock)
                {
                    File.AppendAllText(_config.OutputFilePath, line + Environment.NewLine);
                    Console.WriteLine(_lineCount);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[THREAD ERROR] {ex}");
        }
    }

    private static string GetTimestamp()
    {
        return DateTime.Now.ToString("HH:mm:ss.fff");
    }
}