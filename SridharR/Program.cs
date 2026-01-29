using System;
using System.Threading.Tasks;

namespace ThreadDemo
{
    class Program
    {
        // Keeping Main minimal: eveything Handling FileWriterworker class
        static async Task Main()
        {
            var worker = new FileWriteWorker(
                taskCount: 10,
                writesPerTask: 10);

        await worker.RunAsync();
	    Console.WriteLine($"All Tasks completed, Press Any key to Exit");
        Console.ReadKey();
        }
    }
}
