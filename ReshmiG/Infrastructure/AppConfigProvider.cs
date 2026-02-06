using System;
using System.IO;
using System.Text.Json;

namespace MultiThreadedFileWriter.Infrastructure
{
    internal sealed class AppSettings
    {
        public string? LogDirectory { get; set; }
        public string? LogFileName { get; set; }
        public string? ErrorFileName { get; set; }
        public int? ThreadCount { get; set; }
        public int? LinesPerThread { get; set; }
    }

    internal static class AppSettingsProvider
    {
        public static AppSettings LoadFromFile(string path)
        {
            try
            {
                string? resolvedPath = null;

                if (File.Exists(path))
                {
                    resolvedPath = path;
                }
                else
                {
                    var basePath = AppContext.BaseDirectory ?? Directory.GetCurrentDirectory();
                    var alt = Path.Combine(basePath, path);
                    if (File.Exists(alt)) resolvedPath = alt;
                }

                if (resolvedPath == null) return new AppSettings();

                var json = File.ReadAllText(resolvedPath);
                var cfg = JsonSerializer.Deserialize<AppSettings>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return cfg ?? new AppSettings();
            }
            catch
            {
                return new AppSettings();
            }
        }
    }
}
