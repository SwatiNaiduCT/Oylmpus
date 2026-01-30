# Multithreaded File Writer

## Description
This application demonstrates multithreaded programming in C# with proper synchronization, exception handling, and object-oriented design. It writes to a shared file using 10 threads, ensuring thread-safe access.

## How to Run
1. Build the application:
   ```bash
   dotnet build

# Overview
Console application that initializes a log file and launches multiple threads to append timestamped lines. Intended behavior: create a header line then produce 100 lines from 10 threads (10 lines per thread). Each written line is formatted as: "<lineCount>, <threadId>, <timestamp>".

# Files and responsibilities
Program.cs

Application entry point.
Creates the FileHandler with a file path.
Creates and starts the ThreadManager.
Waits for completion (and, in the provided code, optionally blocks on Console.ReadKey).
Contains a top-level try/catch that logs exceptions.

# FileHandler.cs
Responsible for ensuring the target directory exists, initializing the file, and appending lines.
On construction: determines directory from the file path, creates the directory if needed, and writes an initial header line to the file.
Provides WriteToFile(int lineCount, int threadId) to append a single line with a timestamp.
Uses a private lock to serialize file access.

# ThreadManager.cs
Responsible for creating and managing worker threads.
Maintains a shared integer _lineCount that is incremented as threads produce lines.
Starts 10 threads; each thread writes 10 lines by incrementing the shared counter and calling FileHandler.WriteToFile.
Uses a private lock to synchronize incrementing the counter and writing to the file.
In the provided variant, thread exceptions are captured per-thread and aggregated (AggregateException) after all threads join, so failures are surfaced to the caller.

# How execution flows
Program.Main constructs FileHandler with the configured file path.
FileHandler ensures the directory exists and creates/overwrites the file with a header line ("0, 0, <timestamp>").
Program creates ThreadManager passing the FileHandler.
ThreadManager.StartThreads:
Allocates 10 Thread instances.
Each thread runs a loop that executes 10 iterations:
Acquire ThreadManager lock.
Increment the shared _lineCount.
Call FileHandler.WriteToFile(currentLine, threadId) to append a line.
Release the lock.
The main thread waits for all worker threads to Join.
If any worker captured exceptions, StartThreads throws an AggregateException containing those thread exceptions.
Control returns to Program.Main which handles any exceptions from StartThreads; in the supplied code it logs the exception and exits (Console.ReadKey may block if present and interactive).

# Threading and synchronization
Concurrency primitives:
System.Threading.Thread used to create OS threads.
lock (monitor) used for synchronization.

# Shared state:
_lineCount in ThreadManager is shared and incremented under the ThreadManager lock.
File access is serialized by FileHandler's private lock.
Locking order observed in the code:
ThreadManager acquires its lock, then calls into FileHandler which acquires its own lock. This is the consistent locking sequence in the provided implementation.
Result: the increment of the line counter and the corresponding write call are performed under a single ThreadManager lock, ensuring each line number correlates with its write call from the app's perspective.

# File content and format
First line (header) written during initialization:
"0, 0, <timestamp>" where timestamp format is "HH:mm:ss.fff".
Subsequent lines appended by threads:
"<lineCount>, <threadId>, <timestamp>" where:
lineCount is the incrementing counter starting at 1,
threadId is an integer assigned per thread (1..10 in provided code),
timestamp is current time in "HH:mm:ss.fff".

# Exception handling behavior (current)
FileHandler:
Catches exceptions during initialization and write operations, logs the exception message, and rethrows the exception to the caller.
ThreadManager:
Worker-thread entrypoints catch exceptions and store them in a per-thread slot; after all threads join, StartThreads aggregates captured exceptions and throws an AggregateException if any failures occurred.
Program.Main:
Wraps initialization and StartThreads in a try/catch and logs unhandled exceptions when they propagate to the top level.
Inputs and parameters
File path is provided in Program (example: "/log/out.txt" in the supplied code).
Thread count and lines-per-thread are hard-coded in the provided code (10 threads, 10 writes each).

