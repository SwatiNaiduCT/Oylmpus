namespace ThreadProgram.Core;

public interface ILogger
{
    void Log(string message);
    void LogError(Exception ex);
}