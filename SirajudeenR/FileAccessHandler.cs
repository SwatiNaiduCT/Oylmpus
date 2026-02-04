using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FileAccessor
{
    /// <summary>
    /// Act like a Business Logic handler, which Uses lock-based approach to 
    /// ensure single-threaded file access for synchronized file writes.
    /// </summary>
    public class FileAccessHandler : IDisposable
    {
        private readonly string _filePath;
        private readonly string _dateTimeFormat;
        private readonly object _syncLock = new object();
        private int _writeCounter;
        private bool _disposed;

        public FileAccessHandler(string filePath, string dateTimeFormat = "HH:mm:ss.fff")
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

            _filePath = filePath;
            _dateTimeFormat = dateTimeFormat;
            _writeCounter = 0;
        }

        /// <summary>
        /// Initializes the file system and clears any existing file.
        /// </summary>
        public void Initialize()
        {
            ThrowIfDisposed();

            try
            {
                var directory = Path.GetDirectoryName(_filePath);
                if (string.IsNullOrEmpty(directory))
                    throw new InvalidOperationException("Could not determine directory from the file path.");

                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    Console.WriteLine($"Directory created: {directory}");
                }

                if (File.Exists(_filePath))
                {
                    File.Delete(_filePath);
                    Console.WriteLine($"Existing file deleted: {_filePath}");
                }

                File.WriteAllText(_filePath, string.Empty);
                Console.WriteLine($"File initialized: {_filePath}");
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new InvalidOperationException($"Access denied to path: {_filePath}", ex);
            }
            catch (DirectoryNotFoundException ex)
            {
                throw new InvalidOperationException($"Directory path is invalid", ex);
            }
            catch (IOException ex)
            {
                throw new InvalidOperationException($"I/O error during file initialization: {_filePath}", ex);
            }
        }

        /// <summary>
        /// Thread-safe write operation to the file by allowing access to One thread at a time.
        /// </summary>
        public void WriteEntry(int taskId, int taskWriteCount)
        {
            ThrowIfDisposed();

            lock (_syncLock)
            {
                try
                {
                    _writeCounter++;
                    string timestamp = DateTime.Now.ToString(_dateTimeFormat,CultureInfo.InvariantCulture);
                    string fileEntry = $"{_writeCounter}, {taskId}, {timestamp}";

                    Console.WriteLine($"GlobalCounter={_writeCounter}, TaskId={taskId}, TaskWriteCount={taskWriteCount}");
                    
                    File.AppendAllText(_filePath, fileEntry + Environment.NewLine);
                }
                catch (UnauthorizedAccessException ex)
                {
                    throw new InvalidOperationException($"Access denied when writing to file: {_filePath}", ex);
                }
                catch (IOException ex)
                {
                    throw new InvalidOperationException($"I/O error during file write operation: {_filePath}", ex);
                }
            }
        }

        /// <summary>
        /// Gets the current write counter in a thread-safe manner.
        /// </summary>
        public int GetWriteCounter()
        {
            lock (_syncLock)
            {
                return _writeCounter;
            }
        }

        /// <summary>
        /// Validates that the file exists and is accessible.
        /// </summary>
        public bool ValidateFile()
        {
            try
            {
                return File.Exists(_filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"File validation failed: {ex.Message}");
                return false;
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(FileAccessHandler));
        }

        public void Dispose()
        {
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}