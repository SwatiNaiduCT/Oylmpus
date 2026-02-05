using ThreadProgram.Core;

namespace ThreadProgram.Infrastructure;

public sealed class FileLock : IFileLock
{
    private readonly string _filePath;
    private readonly Mutex _mutex;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public FileLock(string filePath)
    {
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        _mutex = new Mutex(false, $"Global\\FileLock_{filePath.GetHashCode()}");
    }

    public void Execute(Action<FileStream> action)
    {
        bool mutexAcquired = false;

        try
        {
            _semaphore.Wait();

            mutexAcquired = _mutex.WaitOne(TimeSpan.FromSeconds(10));
            if (!mutexAcquired)
                throw new TimeoutException("Timeout acquiring file mutex.");

            using var stream = new FileStream(
                _filePath,
                FileMode.OpenOrCreate,
                FileAccess.ReadWrite,
                FileShare.None);

            action(stream);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new IOException("Access denied while accessing file.", ex);
        }
        finally
        {
            if (mutexAcquired)
                _mutex.ReleaseMutex();

            _semaphore.Release();
        }
    }

    public void Dispose()
    {
        _mutex.Dispose();
        _semaphore.Dispose();
    }
}