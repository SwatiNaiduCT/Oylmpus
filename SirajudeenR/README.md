# File Accessor Application

A .NET 10 console application that creates **10 threads writing to 1 file safely**. Each thread writes **10 times** concurrently, resulting in **100 total writes without data corruption**.

---

## ?? About the Application

- Creates multiple threads that run concurrently
- All threads write to a single shared file
- Uses lock-based synchronization to prevent data corruption
- Demonstrates thread-safe file access patterns

**Key Challenge**: Without proper synchronization, multiple threads cause file corruption.  
**Solution**: Lock mechanism ensures safe, ordered writes while maintaining concurrency.

---

## ?? Core Features

? **Thread-Safe** - Lock prevents file corruption  
? **Concurrent** - 10 threads run at same time  
? **Handles Errors** - Catches problems gracefully  
? **Containerized** - Runs in Docker easily  
? **OOP Design** - Clean, organized code  

---

## ?? Project Files

| File | Purpose |
|------|---------|
| **Program.cs** | Entry point - starts application (7 lines only!) |
| **FileAccessorApplication.cs** | Orchestrates everything - configuration, DI, threads |
| **FileAccessHandler.cs** | Thread-safe file writing with locks |
| **appsettings.json** | Configuration (threads, paths, delays) |
| **AppSettings.cs** | Settings class with strong typing |
| **Dockerfile** | Docker container setup |

### How Files Work Together

**Program.cs** (Entry Point - Super Simple!)
```csharp
using (var application = new FileAccessorApplication())
{
    application.Run();
}
```


**FileAccessorApplication.cs** (The Brain - Does All The Work)
- Loads settings from `appsettings.json`
- Creates dependency injection container
- Initializes file system
- Creates and manages threads
- Displays results
- Handles errors gracefully

**FileAccessHandler.cs** (The Safeguard)
- Uses `lock` for thread-safe writes
- Writes entries with timestamps
- Tracks total number of writes

**appsettings.json** (The Configuration)
- How many threads to create
- How many times each thread writes
- Where to save the output file
- Timestamp format for logging
- Delay between writes (for demonstration)

---

## ?? How to Run Locally

**Prerequisites:**
- .NET 10 SDK installed

**Steps:**
```bash
# Navigate to project folder
cd FileAccessor

# Build
dotnet build

# Run
dotnet run

# Check output
cat /log/out.txt
```

---

## ?? How to Run in Docker

**Prerequisites:**
- Docker installed

**Build and Run:**
```bash
# Build Docker image
docker build -t file-accessor .

# Run with volume for persistent logs
docker run -v file-logs:/log file-accessor:latest

# View logs
docker volume inspect file-logs
```

**Run with Local Folder:**
```bash
# Windows
docker run -i -v C:\MyLogs:/log file-accessor:latest

# Linux/Mac
docker run -i -v /home/user/logs:/log file-accessor:latest
```

---

## ?? Output File Format

Each line in `/log/out.txt` contains:
```
WriteNumber, ThreadId, Timestamp
```

**Example:**
```
1, 1, 14:23:45.123
2, 2, 14:23:45.128
3, 3, 14:23:45.133
...
100, 10, 14:23:50.456
```

---

## ?? Configuration

All settings are in `appsettings.json`. 

```json
{
  "FileAccessorSettings": {
    "TotalNumberOfAllowedThreads": 10,
    "TotalNumberOfAllowedWritesPerThread": 10,
    "FilePath": "/log/out.txt",
    "DatetimeFormat": "HH:mm:ss.fff",
    "ThreadDelayMilliseconds": 5
  }
}
```

### Configuration Examples

**Run 50 threads with 20 writes each (1000 total):**
```json
"TotalNumberOfAllowedThreads": 50
"TotalNumberOfAllowedWritesPerThread": 20
```

**Change output path:**
```json
"FilePath": "C:\\MyLogs\\output.txt"
```

**Detailed timestamps:**
```json
"DatetimeFormat": "yyyy-MM-dd HH:mm:ss.fff"
```

**Increase thread count (1000 threads):**
```json
"TotalNumberOfAllowedThreads": 1000
```

---



###  Solution details
```csharp
lock (sharedResource)  // Only ONE thread can enter at a time
{
    // File write happens here
    // Other threads must wait their turn
}
```

Each thread acquires the lock, writes its data, then releases it. This ensures clean, ordered writes.

---

## ??? Architecture & Design

### Clean Separation of Concerns

**Program.cs** 
```csharp
using (var application = new FileAccessorApplication())
{
    application.Run();
}
```
 Just instantiate and run.

**FileAccessorApplication.cs** (Orchestrator - 200+ lines)
- Manages application lifecycle
- Loads configuration from appsettings.json
- Sets up dependency injection
- Creates and coordinates threads
- Handles errors gracefully
- Cleans up resources with IDisposable

**FileAccessHandler.cs** (File Operations)
- Encapsulates all file I/O
- Uses `lock` for thread synchronization
- Counts and validates writes

### Flow Diagram

```
Program.Main()
  ?? new FileAccessorApplication()
       ?? Load appsettings.json
       ?? Create Dependency Injection
       ?? Register AppSettings & FileAccessHandler
       ?
       ?? Run()
            ?? InitializeFileSystem()
            ?   ?? Create/validate log file
            ?
            ?? RunThreads()
            ?   ?? Create 10 Thread objects
            ?   ?? Start each thread
            ?   ?? Each thread: WriteToFile(threadId)
            ?   ?   ?? For each write:
            ?   ?      ?? Lock ? Write ? Release
            ?   ?? Wait for all with Join()
            ?
            ?? DisplayCompletionInfo()
                ?? Show results & file path
```

### Why This Design?

? **Testable** - FileAccessorApplication can be unit tested  
? **Maintainable** - Logic in clear, organized methods  
? **Reusable** - Can use from any entry point  
? **Clean** - Program.cs is only 7 lines!  
? **Flexible** - Easy to add features or modify behavior  

---

## ?? Thread Synchronization Details

### FileAccessHandler Class
Encapsulates all file operations:
- `Initialize()` - Sets up file and directory
- `WriteEntry(threadId, count)` - Thread-safe write with lock
- `ValidateFile()` - Checks file accessibility
- `GetWriteCounter()` - Returns total writes
- `Dispose()` - Cleanup and resource management

### Exception Handling Layers
1. **Application Level** (Run method) - Catches main errors
2. **Thread Level** (WriteToFile method) - Catches thread-specific errors
3. **Handler Level** (FileAccessHandler class) - Handles I/O errors

---

```bash
# Check .NET version
dotnet --version

# Build locally
dotnet build

# Run locally
dotnet run

# Build Docker image
docker build -t file-accessor .

# List Docker images
docker images

# Run container
docker run -v file-logs:/log file-accessor:latest

# View container logs
docker logs <container-id>

# See volumes
docker volume ls

# Delete volume
docker volume rm file-logs

# Stop container
docker stop <container-id>
```

---

## ?? Architecture

### FileAccessHandler Class
Encapsulates all file operations:
- `Initialize()` - Sets up file and directory
- `WriteEntry(threadId, count)` - Thread-safe write with lock
- `ValidateFile()` - Checks file accessibility
- `GetWriteCounter()` - Returns total writes
- `Dispose()` - Cleanup and resource management

### Exception Handling Layers
1. **Application Level** - Catches main errors
2. **Thread Level** - Catches thread-specific errors
3. **Handler Level** - Handles I/O errors

---

## ? Troubleshooting

| Problem | Solution |
|---------|----------|
| "Access Denied" error | Check `/log` directory permissions |
| App won't start | Verify .NET 10 is installed |
| Docker won't run | Ensure Docker daemon is running |
| No output file created | Check app runs to completion |
| Port conflicts | Use different file path |

---

## ?? Key Concepts Demonstrated

? Thread synchronization with locks  
? Concurrent execution  
? Exception handling  
? Resource management (IDisposable)  
? File I/O operations  
? Docker containerization  
? OOP design patterns  
? Dependency injection  
? Configuration management  

---


