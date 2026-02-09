using System.Threading;
using Tasklog.Infrastructure;

namespace Tasklog.Services;

public class Worker
{
    private readonly ConcurrentFileWriter _writer;
    private readonly int _writesPerThread;

    public Worker(ConcurrentFileWriter writer, int writesPerThread)
    {
        _writer = writer;
        _writesPerThread = writesPerThread;
    }

    public void Run()
    {
        int threadId = Thread.CurrentThread.ManagedThreadId;

        for (int i = 0; i < _writesPerThread; i++)
        {
            _writer.WriteNextLine(threadId);
        }
    }
}