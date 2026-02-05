using System;
using System.IO;

namespace MultiThreadedFileWriter.Infrastructure
{
    /// <summary>
    /// Simple thread-safe file logger that appends lines to a text file.
    /// Synchronization is done with a private lock to avoid interleaved writes.
    /// </summary>
    public sealed class FileLogger : IFileLogger
    {
        private readonly string _filePath;
        private readonly object _writeLock = new object();
        private bool _disposed;

        public FileLogger(string filePath)
        {
            _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));

            var dir = Path.GetDirectoryName(_filePath);
            if (string.IsNullOrWhiteSpace(dir))
                throw new ArgumentException("filePath must include a directory component", nameof(filePath));

            Directory.CreateDirectory(dir);
        }

        public void InitializeFirstLine()
        {
            var ts = DateTime.Now.ToString("HH:mm:ss.fff");
            var firstLine = $"0, 0, {ts}" + Environment.NewLine;

            lock (_writeLock)
            {
                File.WriteAllText(_filePath, firstLine);
            }
        }

        public void AppendLine(long lineCount, int threadId, string timestamp)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(FileLogger));

            var line = $"{lineCount}, {threadId}, {timestamp}" + Environment.NewLine;

            lock (_writeLock)
            {
                File.AppendAllText(_filePath, line);
            }
        }

        public void Dispose()
        {
            _disposed = true;
        }
    }
}
