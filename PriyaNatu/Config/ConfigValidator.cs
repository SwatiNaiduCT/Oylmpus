namespace Tasklog.Config;

public static class ConfigurationValidator
{
    public static void Validate(AppConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.OutputFilePath))
            throw new ArgumentException("OutputFilePath must be provided.");

        if (config.ThreadCount <= 0)
            throw new ArgumentException("ThreadCount must be greater than 0.");

        if (config.WritesPerThread <= 0)
            throw new ArgumentException("WritesPerThread must be greater than 0.");
    }
}