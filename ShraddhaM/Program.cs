namespace MultithreadedFileWriter;
using System;

class Program
{
    static int Main(string[] args)
    {
        string filePath = "/log/out.txt";

        try
        {
            var fileHandler = new FileHandler(filePath);
            var threadManager = new ThreadManager(fileHandler);
            threadManager.StartThreads();

            Console.WriteLine("All threads have completed. Press any key to exit...");
            Console.ReadKey();
            return 0; // success
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Unhandled exception: {ex.ToString()}");
            return 1; // failure
        }
    }
}