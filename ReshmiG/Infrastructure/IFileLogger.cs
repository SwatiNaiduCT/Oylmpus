using System;

namespace MultiThreadedFileWriter.Infrastructure
{
    /// <summary>
    /// Interface for the file logger used by the sample application.
    /// </summary>
    public interface IFileLogger : IDisposable
    {
        /// <summary>
        /// Create or overwrite the target file with the initial line ("0, 0, timestamp").
        /// </summary>
        void InitializeFirstLine();

        /// <summary>
        /// Append a single formatted line to the log file.
        /// </summary>
        /// <param name="lineCount">Global line counter value.</param>
        /// <param name="threadId">Managed thread id writing the line.</param>
        /// <param name="timestamp">Formatted timestamp string.</param>
        void AppendLine(long lineCount, int threadId, string timestamp);
    }
}
