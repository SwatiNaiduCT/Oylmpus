using System;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FileAccessor
{
    /// <summary>
    /// Orchestrates the file accessor application lifecycle
    /// Handles configuration, dependency injection, and thread coordination
    /// </summary>
    public class FileAccessorApplication : IDisposable
    {
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;
        private readonly AppSettings _settings;
        private FileAccessHandler _fileHandler;
        private bool _disposed;

        public FileAccessorApplication()
        {
            // Build configuration
            _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            // Setup dependency injection
            var services = new ServiceCollection();
            services.Configure<AppSettings>(_configuration.GetSection("FileAccessorSettings"));
            services.AddSingleton(_configuration);
            
            // Register FileAccessHandler with settings from configuration
            services.AddSingleton(sp =>
            {
                var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<AppSettings>>().Value;
                return new FileAccessHandler(settings.FilePath, settings.DatetimeFormat);
            });
            
            _serviceProvider = services.BuildServiceProvider();

            // Get settings from DI
            _settings = _serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<AppSettings>>().Value;
        }

        /// <summary>
        /// Runs the application - initializes file handler and starts threads
        /// </summary>
        public void Run()
        {
            ThrowIfDisposed();

            try
            {
                // Get file handler from DI
                _fileHandler = _serviceProvider.GetRequiredService<FileAccessHandler>();

                // Initialize file system
                InitializeFileSystem();

                // Run threads
                RunThreads();

                // Display completion info
                DisplayCompletionInfo();
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        /// <summary>
        /// Initializes the file system and validates file creation
        /// </summary>
        private void InitializeFileSystem()
        {
            _fileHandler.Initialize();

            if (!_fileHandler.ValidateFile())
            {
                throw new InvalidOperationException("File validation failed after initialization.");
            }

            Console.WriteLine($"File system initialized: {_settings.FilePath}\n");
        }

        /// <summary>
        /// Creates and manages concurrent threads for file writing
        /// </summary>
        private void RunThreads()
        {
            Console.WriteLine($"Starting {_settings.TotalNumberOfAllowedThreads} concurrent threads to write to shared file\n");

            // Create and start threads
            Thread[] threads = new Thread[_settings.TotalNumberOfAllowedThreads];
            for (int i = 0; i < _settings.TotalNumberOfAllowedThreads; i++)
            {
                int threadId = i + 1;
                threads[i] = CreateThread(threadId);
                threads[i].Start();
            }
            
            Console.WriteLine($"All {_settings.TotalNumberOfAllowedThreads} threads created and running concurrently\n");

            // Wait for all threads to complete
            foreach (var thread in threads)
            {
                thread.Join();
            }
        }

        /// <summary>
        /// Creates a thread for file writing with proper naming and configuration
        /// </summary>
        private Thread CreateThread(int threadId)
        {
            return new Thread(() => WriteToFile(threadId))
            {
                Name = $"FileWriter-{threadId}",
                IsBackground = true
            };
        }

        /// <summary>
        /// Thread write method - writes entries to file
        /// </summary>
        private void WriteToFile(int threadId)
        {
            try
            {
                for (int i = 0; i < _settings.TotalNumberOfAllowedWritesPerThread; i++)
                {
                    int currentWrite = i + 1;
                    _fileHandler.WriteEntry(threadId, currentWrite);
                    
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

        /// <summary>
        /// Displays application Final status
        /// </summary>
        private void DisplayCompletionInfo()
        {
            Console.WriteLine($"\nAll threads completed");
            Console.WriteLine($"Total writes: {_fileHandler.GetWriteCounter()}");
            Console.WriteLine($"Output file: {_settings.FilePath}");
            Console.WriteLine("\nPress any key to exit");
            Console.ReadLine();
        }

        /// <summary>
        /// Handles exceptions with formatted output
        /// </summary>
        private void HandleException(Exception ex)
        {
            Console.WriteLine($"\nError: {ex.GetType().Name}: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"   Inner exception: {ex.InnerException.Message}");
            }
            Console.WriteLine($"\n   Stack trace: {ex.StackTrace}");
        }

        /// <summary>
        /// Checks if object has been disposed
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(FileAccessorApplication));
        }

        /// <summary>
        /// Disposes resources
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;

            _fileHandler?.Dispose();
            (_serviceProvider as IDisposable)?.Dispose();
            
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
