using System;
using System.Collections.Concurrent;
using System.Threading;

namespace ThreadedFileWriter
{
    /// <summary>
    /// this worker writes a set number of lines to the shared file.
    /// Any errors inside the thread are caught and added to a shared error list.
    /// </summary>
    public sealed class LineWriterWorker
    {
        private readonly FileAppender _appender;
        private readonly int _writes;
        private readonly ConcurrentQueue<Exception> _errors;

        public LineWriterWorker(FileAppender appender, int writes, ConcurrentQueue<Exception> errors)
        {
            if (appender == null)
                throw new ArgumentNullException("appender");

            _appender = appender;

            _writes = writes;

            if (errors == null)
                throw new ArgumentNullException("errors");

            _errors = errors;
        }

        public void Run()
        {
            try
            {
                int threadId = Thread.CurrentThread.ManagedThreadId;

                for (int i = 0; i < _writes; i++)
                {
                    _appender.AppendNext(threadId);
                }
            }
            catch (Exception ex)
            {
                _errors.Enqueue(new Exception(
                    $"Thread '{Thread.CurrentThread.Name}' (Id={Thread.CurrentThread.ManagedThreadId}) failed.",
                    ex));
            }
        }
    }
}