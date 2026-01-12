
string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
string folderPath = Path.GetFullPath(Path.Combine(baseDirectory, @"../../../log/"));
string filePath = folderPath + "out.txt";
const int NumThreads = 10;
const int LinesPerThread = 10;
object fileLock = new();

try
{
    string timeStamp = DateTime.Now.ToString("HH:mm:ss.fff");
    File.WriteAllText(filePath, "0, 0, " + timeStamp + Environment.NewLine);

    // Create a list to hold the tasks
    List<Task> tasks = new List<Task>();

    //Launch 10 threads to run simultaneously.
    for (int i = 0; i < NumThreads; i++)
    {
        int threadId = i + 1; // Local copy of loop variable for the lambda
        tasks.Add(Task.Run(() => WriteToFile(threadId))); //Create and start a new Task for each iteration
    }
    Task.WhenAll(tasks).Wait();
    Console.ReadKey();
}
catch (Exception e)
{
    Console.WriteLine($"An error occurred: {e.Message}");
}

// The method that each thread will execute
void WriteToFile(int threadId)
{
    for (int i = 1; i <= LinesPerThread; i++)
    {
        //“< line_count >, < thread_id >, < current_time_stamp >” 
        string line = $"{i}, {threadId}, {DateTime.Now:HH:mm:ss.fff}";

        // Synchronize access to the file with a lock
        lock (fileLock)
        {
            File.AppendAllText(filePath, line + Environment.NewLine);
        }
    }
}

