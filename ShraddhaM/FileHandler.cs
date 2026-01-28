namespace MultithreadedFileWriter;

using System;
using System.IO;
using System.Text;

public class FileHandler
{
private readonly string _filePath;
    private readonly object _lock = new object();

    public FileHandler(string filePath)
    {
        _filePath = filePath;

        // Ensure the directory exists
        var directory = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Initialize the file
        InitializeFile();
    }

    private void InitializeFile()
    {
        try
        {
            lock (_lock)
            {
                using (var writer = new StreamWriter(_filePath, false, Encoding.UTF8))
                {
                    string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                    writer.WriteLine($"0, 0, {timestamp}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing file: {ex.Message}");
            throw;
        }
    }

    public void WriteToFile(int lineCount, int threadId)
    {
        try
        {
            lock (_lock)
            {
                using (var writer = new StreamWriter(_filePath, true, Encoding.UTF8))
                {
                    string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                    writer.WriteLine($"{lineCount}, {threadId}, {timestamp}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error writing to file: {ex.Message}");
            throw;
        }
 
}
}
