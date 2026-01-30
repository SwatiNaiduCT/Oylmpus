using System;
using System.IO;
using System.Text;

namespace MultithreadedFileWriter
{
    public class FileHandler
    {
        private readonly string _filePath;
        private readonly object _lock = new object();

        public FileHandler(string filePath)
        {
            _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));

            // Normalize directory: Path.GetDirectoryName can return null (e.g. "out.txt")
            var directory = Path.GetDirectoryName(_filePath);
            if (string.IsNullOrWhiteSpace(directory))
            {
                // If caller passed a file name without directory, use current directory
                directory = Directory.GetCurrentDirectory();
                _filePath = Path.Combine(directory, Path.GetFileName(_filePath));
            }

            try
            {
                Directory.CreateDirectory(directory); // safe if already exists
            }
            catch (Exception ex) when (
                ex is IOException ||
                ex is UnauthorizedAccessException ||
                ex is ArgumentException ||
                ex is NotSupportedException ||
                ex is PathTooLongException)
            {
                // Log with full details and rethrow a clearer exception (preserve inner)
                Console.Error.WriteLine($"Failed to create directory '{directory}': {ex}");
                throw new IOException($"Could not create directory '{directory}' for file '{_filePath}'.", ex);
            }

            // Create/initialize the file (overwrite) and write initial header line
            InitializeFile();
        }

        private void InitializeFile()
        {
            // Keep locking consistent with WriteToFile
            lock (_lock)
            {
                try
                {
                    // Use FileStream so behavior is explicit (create/overwrite)
                    using (var fs = new FileStream(
                        _filePath,
                        FileMode.Create,
                        FileAccess.Write,
                        FileShare.Read,
                        bufferSize: 4096,
                        FileOptions.WriteThrough))
                    using (var writer = new StreamWriter(fs, Encoding.UTF8))
                    {
                        string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                        writer.WriteLine($"0, 0, {timestamp}");
                    }
                }
                catch (Exception ex) when (
                    ex is IOException ||
                    ex is UnauthorizedAccessException)
                {
                    Console.Error.WriteLine($"Error initializing file '{_filePath}': {ex}");
                    throw; // preserve original exception and stack trace
                }
            }
        }

        public void WriteToFile(int lineCount, int threadId)
        {
            lock (_lock)
            {
                try
                {
                    // Append mode with explicit FileMode and FileShare
                    using (var fs = new FileStream(
                        _filePath,
                        FileMode.Append,
                        FileAccess.Write,
                        FileShare.Read,
                        bufferSize: 4096,
                        FileOptions.WriteThrough))
                    using (var writer = new StreamWriter(fs, Encoding.UTF8))
                    {
                        string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                        writer.WriteLine($"{lineCount}, {threadId}, {timestamp}");
                    }
                }
                catch (Exception ex) when (
                    ex is IOException ||
                    ex is UnauthorizedAccessException)
                {
                    Console.Error.WriteLine($"Error writing to file '{_filePath}': {ex}");
                    throw; // let caller decide how to handle
                }
            }
        }

        // Optional helper that attempts to write and returns success/failure instead of throwing
        public bool TryWrite(int lineCount, int threadId)
        {
            try
            {
                WriteToFile(lineCount, threadId);
                return true;
            }
            catch
            {
                // Already logged inside WriteToFile
                return false;
            }
        }
    }
}