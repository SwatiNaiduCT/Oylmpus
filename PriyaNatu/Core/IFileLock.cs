namespace ThreadProgram.Core;

public interface IFileLock : IDisposable
{
    void Execute(Action<FileStream> action);
}