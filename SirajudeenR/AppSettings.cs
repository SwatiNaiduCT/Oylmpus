namespace FileAccessor
{
    /// <summary>
    /// Configuration settings for FileAccessor application.
    /// Binds to "FileAccessorSettings" section in appsettings.json
    /// </summary>
    public class AppSettings
    {
        public int TotalNumberOfAllowedThreads { get; set; } = 10;

        public int TotalNumberOfAllowedWritesPerThread { get; set; } = 10;

        public string FilePath { get; set; } = "/log/out.txt";

        public string DatetimeFormat { get; set; } = "HH:mm:ss.fff";

    }
}
