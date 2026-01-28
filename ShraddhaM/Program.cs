namespace MultithreadedFileWriter;
using System;

class Program
{
    static void Main(string[] args)
    {
        string filePath = "/log/out.txt";

        try
        {
            // Initialize the file handler
            var fileHandler = new FileHandler(filePath);

            // Start the threads
            var threadManager = new ThreadManager(fileHandler);
            threadManager.StartThreads();

            Console.WriteLine("All threads have completed. Press any key to exit...");
            Console.ReadKey();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unhandled exception: {ex.Message}");
        }
    }
}