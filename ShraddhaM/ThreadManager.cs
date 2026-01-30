using System;
using System.Threading;

namespace MultithreadedFileWriter
{
    public class ThreadManager
    {
        private readonly FileHandler _fileHandler;
        private int _lineCount = 0; // Start from 0 as per the requirement
        private readonly object _lock = new object(); // Lock object for synchronization

        public ThreadManager(FileHandler fileHandler)
        {
            _fileHandler = fileHandler ?? throw new ArgumentNullException(nameof(fileHandler));
        }

        /// <summary>
        /// Starts 10 threads, waits for them to finish and propagates any thread exceptions
        /// as an AggregateException back to the caller.
        /// </summary>
        public void StartThreads()
        {
            const int threadCount = 10;
            Thread[] threads = new Thread[threadCount];
            Exception[] threadExceptions = new Exception[threadCount];

            for (int i = 0; i < threadCount; i++)
            {
                int idx = i;                 // capture loop variable
                int threadId = i + 1;        // human-friendly thread id

                threads[i] = new Thread(() =>
                {
                    try
                    {
                        WriteToFile(threadId);
                    }
                    catch (Exception ex)
                    {
                        // Capture the exception for later propagation
                        threadExceptions[idx] = ex;
                    }
                });

                threads[i].Start();
            }

            // Wait for all threads to complete
            foreach (var thread in threads)
            {
                thread.Join();
            }

            // After all threads have completed, propagate any exceptions
            var exceptionsToThrow = new System.Collections.Generic.List<Exception>();
            for (int i = 0; i < threadExceptions.Length; i++)
            {
                if (threadExceptions[i] != null)
                    exceptionsToThrow.Add(threadExceptions[i]);
            }

            if (exceptionsToThrow.Count > 0)
            {
                throw new AggregateException("One or more threads failed.", exceptionsToThrow);
            }
        }

        /// <summary>
        /// Performs the file writes. Any exceptions are allowed to bubble up to the caller
        /// (StartThreads captures them and rethrows after all threads join).
        /// </summary>
        private void WriteToFile(int threadId)
        {
            // Each thread writes exactly 10 lines
            for (int i = 0; i < 10; i++)
            {
                int currentLine;

                // Synchronize access to the shared resource
                lock (_lock)
                {
                    currentLine = ++_lineCount; // Increment line count safely
                    _fileHandler.WriteToFile(currentLine, threadId); // Write to file
                }
            }
        }
    }
}