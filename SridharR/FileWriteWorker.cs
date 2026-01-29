using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ThreadDemo
{
    public sealed class FileWriteWorker 
    {
        private readonly int _taskCount;
        private readonly int _writesPerTask;

        private readonly object _lockObj = new object();
        private int _lineCounter = 0;

        private Barrier _barrier;
        
        //Constructor to Assign Required information from Main
        public FileWriteWorker(int taskCount, int writesPerTask)
        {
            if (taskCount <= 0) throw new ArgumentOutOfRangeException(nameof(taskCount));
            if (writesPerTask <= 0) throw new ArgumentOutOfRangeException(nameof(writesPerTask));

            _taskCount = taskCount;
            _writesPerTask = writesPerTask;
            _barrier = new Barrier(_taskCount);
        }

        //Method That Handles Task Creations and Running the Task
        public async Task RunAsync()
        {
            string writePath = GetWritePath(); // user-writable
            Console.WriteLine($"Writing to: {writePath}");

            try
            {
                // Ensure directory exists and initialize file with first line
                Directory.CreateDirectory(Path.GetDirectoryName(writePath)!);
                using (var initWriter = new StreamWriter(writePath, false))
                {
                    initWriter.WriteLine($"0, 0, {DateTime.Now:HH:mm:ss.fff}");
                }

                var tasks = new List<Task>(_taskCount);
                for (int i = 0; i < _taskCount; i++)
                {
                    tasks.Add(Task.Run(() => DoWork(writePath)));
                }

                await Task.WhenAll(tasks);

                Console.WriteLine($"All tasks completed. Total lines: {_lineCounter} (expected {_taskCount * _writesPerTask}).");
            }

            catch (UnauthorizedAccessException ex)
            {
                Console.Error.WriteLine("UnauthorizedAccessException: " + ex.Message);
                Console.Error.WriteLine(ex);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Unexpected error: " + ex);
            }
        }

        private void DoWork(string writePath)
        {
            int threadId = Environment.CurrentManagedThreadId;

            // Wait until all tasks reach the barrier
            _barrier.SignalAndWait();

            // Critical section (counter + write paired atomically)
            for (int w = 0; w < _writesPerTask; w++)
            {
                lock (_lockObj)
                {
                    int lineNo = ++_lineCounter;
                    using (var sharedWriter = new StreamWriter(writePath, append: true))
                    {
                        sharedWriter.WriteLine($"{lineNo}, {threadId}, {DateTime.Now:HH:mm:ss.fff}");
                    }
                }
            }
        }

        // Use a user-writable location to avoid UnauthorizedAccessException
         private static string GetWritePath()
        {
            string appDir;
            if (OperatingSystem.IsWindows())
            {
                appDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            }
            else
            {
                appDir = "/olympus/out.txt";
		        return appDir;
            }

            return Path.Combine(appDir, "out.txt");
        }

    }
}
