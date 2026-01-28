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
            _fileHandler = fileHandler;
        }

        public void StartThreads()
        {
            Thread[] threads = new Thread[10];

            for (int i = 0; i < 10; i++)
            {
                int threadId = i + 1; // Assign a unique ID to each thread
                threads[i] = new Thread(() => WriteToFile(threadId));
                threads[i].Start();
            }

            // Wait for all threads to complete
            foreach (var thread in threads)
            {
                thread.Join();
            }
        }

        private void WriteToFile(int threadId)
        {
            try
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
            catch (Exception ex)
            {
                Console.WriteLine($"Error in thread {threadId}: {ex.Message}");
            }
        }
    }
}