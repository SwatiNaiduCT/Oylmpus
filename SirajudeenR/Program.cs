//using System.Runtime.CompilerServices;
//using System.Runtime.InteropServices.Marshalling;

using System;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FileHandler;

namespace FileAccessor
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("File Accessor Application Started");

            // Build configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            // Setup dependency injection
            var services = new ServiceCollection();
            services.Configure<AppSettings>(configuration.GetSection("FileAccessorSettings"));
            services.AddSingleton(configuration);
            
            // Register FileAccessHandler with settings from configuration
            services.AddSingleton(sp =>
            {
                var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<AppSettings>>().Value;
                return new FileAccessHandler(settings.FilePath, settings.DatetimeFormat);
            });
            
            var serviceProvider = services.BuildServiceProvider();

            // Get settings from DI
            var settings = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<AppSettings>>().Value;

            FileAccessHandler fileHandler = null;

            try
            {
                // Get file handler from DI
                fileHandler = serviceProvider.GetRequiredService<FileAccessHandler>();

                // Initialize file system
                fileHandler.Initialize();

                // Validate file creation
                if (!fileHandler.ValidateFile())
                {
                    throw new InvalidOperationException("File validation failed after initialization.");
                }

                //Since File access or handling is Input Output bound operation,
                //we can preferably use Task based programming  which internally uses .NET  Thread pool.
                //The Task releases the thread from threadpool automatically once the task is done.
                //here we achive Concurrency handling. The same can be achived with Thread level implementation with manual control like below
                Console.WriteLine($"Starting {settings.TotalNumberOfAllowedThreads} concurrently to write to the shared file");

                // Create and run concurrent threads
                Thread[] threads = new Thread[settings.TotalNumberOfAllowedThreads];
                for (int i = 0; i < settings.TotalNumberOfAllowedThreads; i++)
                {
                    int threadId = i + 1;
                    threads[i] = new Thread(() => WriteToFile(fileHandler, threadId, settings))
                    {
                        Name = $"FileWriter-{threadId}",
                        IsBackground = true
                    };
                    threads[i].Start();
                    Console.WriteLine($"Thread {threadId} started");
                }
                
                Console.WriteLine($"All {settings.TotalNumberOfAllowedThreads} threads created and running concurrently");

                // Wait for all threads to complete
                foreach (var thread in threads)
                {
                    thread.Join();
                }

                Console.WriteLine($"All threads completed");
                Console.WriteLine($"Total writes: {fileHandler.GetWriteCounter()}");
                Console.WriteLine("Press any key to exit");
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.GetType().Name}: {ex.Message}");
                if (ex.InnerException != null)
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            finally
            {
                fileHandler?.Dispose();
                serviceProvider?.Dispose();
            }
        }

        /// <summary>
        /// Synchronous thread method for writing to file with exception handling per thread.
        /// </summary>
        private static void WriteToFile(FileAccessHandler fileHandler, int threadId, AppSettings settings)
        {
            try
            {
                for (int i = 0; i < settings.TotalNumberOfAllowedWritesPerThread; i++)
                {
                    int currentWrite = i + 1;
                    fileHandler.WriteEntry(threadId, currentWrite);
                    
                }
                Console.WriteLine($"Thread {threadId} finished all writes");
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"Thread {threadId} failed - file access error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Thread {threadId} failed - unexpected error: {ex.GetType().Name}: {ex.Message}");
            }
        }
    }
}
