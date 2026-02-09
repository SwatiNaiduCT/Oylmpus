using System.Text;

namespace Tasklog.Infrastructure;

public sealed class ConcurrentFileWriter : IDisposable
{
    private readonly object _lock = new();
    private readonly StreamWriter _writer;
    private int _lineCount;

    public ConcurrentFileWriter(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory!);

        _writer = new StreamWriter(
            new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None),
            Encoding.UTF8,
            bufferSize: 4096,
            leaveOpen: false
        )
        {
            AutoFlush = true
        };

        // Initialize first line
        WriteInternal(0, 0);
    }

    public void WriteNextLine(int threadId)
    {
        lock (_lock)
        {
            _lineCount++;
            WriteInternal(_lineCount, threadId);
        }
    }

    private void WriteInternal(int lineNumber, int threadId)
    {
        string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        _writer.WriteLine($"{lineNumber}, {threadId}, {timestamp}");
    }

    public void Dispose()
    {
        _writer.Dispose();
    }
}