namespace Tasklog.Config;

public class AppConfig
{
    public string OutputFilePath { get; set; } = "/log/out.txt";
    public int ThreadCount { get; set; } = 10;
    public int WritesPerThread { get; set; } = 10;
}