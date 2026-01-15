using System.Collections.Concurrent;

namespace OylmpusAssignment
{
    internal static class Program
    {
        private const int numberOfThreads = 10;
        private const int linesPerThread = 10;
        private const int boundedCapacity = 100; // backpressure to keep memory bounded
        private static readonly string folderPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"../../../log/"));
        private static readonly string logPath = folderPath + "out.txt";
        static void Main(string[] args)
        {
            Console.WriteLine("Starting threading demo (10 threads × 10 lines)...");

            using var cts = new CancellationTokenSource();
            var token = cts.Token;

            // Shared thread-safe collection
            using var queue = new BlockingCollection<WorkItem>(new ConcurrentQueue<WorkItem>(), boundedCapacity);

            // Ctrl+C graceful cancellation
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                Console.WriteLine("Cancellation requested. Attempting graceful shutdown...");
                cts.Cancel();
                try { queue.CompleteAdding(); } catch { /* ignore */ }
            };

            // Start consumer for single file writing
            var consumerThread = new Thread(() => ConsumerLoop(queue, logPath, token))
            {
                Name = "Consumer",
                IsBackground = false
            };
            consumerThread.Start();

            // Start the producer tasks in parallel
            var producers = new List<Thread>(numberOfThreads);
            for (int p = 0; p < numberOfThreads; p++)
            {
                var t = new Thread(() => ProducerLoop(linesPerThread, queue, cts))
                {
                    Name = $"Producer-{p}",
                    IsBackground = false
                };
                producers.Add(t);
                t.Start();
            }

            // Wait for all producers
            foreach (var t in producers)
            {
                t.Join();
            }

            // Signal no more items
            if (!queue.IsAddingCompleted)
            {
                queue.CompleteAdding();
            }

            // Wait for consumer to drain and finish
            consumerThread.Join();

            Console.WriteLine("Done. Press any key to exit.");
            Console.ReadKey();
        }

        private static void ProducerLoop(int messagesPerProducer, BlockingCollection<WorkItem> queue, CancellationTokenSource cts)
        {
            try
            {
                int producerThreadId = Environment.CurrentManagedThreadId;

                for (int i = 1; i <= messagesPerProducer; i++)
                {
                    if (cts.IsCancellationRequested)
                        break;

                    var item = new WorkItem(producerThreadId, i, DateTime.UtcNow);
                    queue.Add(item, cts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine($"[{Thread.CurrentThread.Name}] canceled.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{Thread.CurrentThread.Name}] error: {ex.Message}");
                cts.Cancel();
                try { queue.CompleteAdding(); } catch { /* ignore */ }
            }
        }

        private static void ConsumerLoop(BlockingCollection<WorkItem> queue, string logPath, CancellationToken token)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(logPath))!);

                using var fs = new FileStream(logPath, FileMode.Create, FileAccess.Write, FileShare.Read);
                using var sw = new StreamWriter(fs) { AutoFlush = true };

                long lineNumber = 0;

                sw.WriteLine("0,\t0,\t" + DateTime.Now.ToString("HH:mm:ss.fff"));

                foreach (var item in queue.GetConsumingEnumerable(token))
                {
                    long next = Interlocked.Increment(ref lineNumber);

                    // Output: LineNumber, ProducerThreadId, CreatedUtc
                    sw.WriteLine($"{next},\t{item.ProducerThreadId},\t{item.CreatedUtc:HH:mm:ss.fff}");
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("[Consumer] Canceled.");
            }
            catch (IOException ioEx)
            {
                Console.WriteLine($"[Consumer] I/O error: {ioEx.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Consumer] error: {ex.Message}");
            }
        }
    }

    public sealed class WorkItem
    {
        public int ProducerThreadId { get; }
        public int ProducerSequence { get; }
        public DateTime CreatedUtc { get; }

        public WorkItem(int producerThreadId, int producerSequence, DateTime createdUtc)
        {
            ProducerThreadId = producerThreadId;
            ProducerSequence = producerSequence;
            CreatedUtc = createdUtc;
        }
    }
}
