using ThreadProgram.Core;

namespace ThreadProgram.Infrastructure;

public class Logger : ILogger
{
    public void Log(string message)
        => Console.WriteLine($"[INFO] {message}");

    public void LogError(Exception ex)
        => Console.WriteLine($"[ERROR] {ex}");
}