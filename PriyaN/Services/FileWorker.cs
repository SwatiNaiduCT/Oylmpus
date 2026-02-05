using System.Text;
using ThreadProgram.Core;

namespace ThreadProgram.Services;

public class FileWorker
{
    private readonly IFileLock _fileLock;
    private readonly ILogger _logger;

    public FileWorker(IFileLock fileLock, ILogger logger)
    {
        _fileLock = fileLock;
        _logger = logger;
    }

    public Task RunAsync(CancellationToken token)
    {
        return Task.Run(() =>
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    _fileLock.Execute(stream =>
                    {
                        var content = $"Written at {DateTime.UtcNow}\n";
                        var bytes = Encoding.UTF8.GetBytes(content);

                        stream.Seek(0, SeekOrigin.End);
                        stream.Write(bytes);
                    });

                    _logger.Log("Write completed.");
                    Thread.Sleep(1000);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }
        }, token);
    }
}