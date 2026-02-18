using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

namespace ThreadedFileWriter
{
    /// <summary>
    /// Thread-safe file appender.
    /// </summary>
    public sealed class FileAppender : IDisposable
    {
        private readonly FileStream _stream;
        private readonly StreamWriter _writer;
        private readonly object _syncRoot = new();
        private int _nextLineNumber = 1;

        public string FilePath { get; }

        public FileAppender(string filePath)
        {
            FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));

            // Create or replace the file and keep it open for writing.
            // Allow other processes to read while we're writing.
            _stream = new FileStream(
                FilePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.Read,
                bufferSize: 4096,
                FileOptions.SequentialScan);

            _writer = new StreamWriter(_stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false))
            {
                AutoFlush = true
            };
        }

        /// <summary>
        /// Writes the first line "0, 0, <timestamp>".
        /// </summary>
        public void InitializeWithHeader()
        {
            lock (_syncRoot)
            {
                _writer.WriteLine($"0, 0, {CurrentTimestamp()}");
            }
        }

        /// <summary> 
        /// Makes sure that only one thread writes to the file at a time.
        /// Inside the lock, we get the next line number and write the line.
        /// After the write succeeds, we increase the line number.
        /// This prevents threads from writing at the same time and messing up the order.
        /// </summary>
        public void AppendNext(int threadId)
        {
            lock (_syncRoot)
            {
                // get the current line number
                int lineNumber = _nextLineNumber;

                string line = $"{lineNumber}, {threadId}, {CurrentTimestamp()}";
                // write to the file
                _writer.WriteLine(line);

                // increase the number only if writing worked
                _nextLineNumber++;
            }
        }

        private static string CurrentTimestamp()
            => DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);

        public void Dispose()
        {
            try
            {
                _writer?.Flush();
            }
            catch { }

            _writer?.Dispose();
            _stream?.Dispose();
        }
    }
}