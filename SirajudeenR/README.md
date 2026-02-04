# File Accessor Application

A .NET 10 console application that creates **10 threads writing to 1 file safely**. Each thread writes **10 times** concurrently, resulting in **100 total writes without data corruption**.

---

## ?? What Does This App Do?

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
| **Program.cs** | Main app logic (creates 10 threads) |
| **FileAccessHandler.cs** | Handles file writing safely |
| **appsettings.json** | Configuration settings |
| **AppSettings.cs** | Settings class for strong typing |
| **Dockerfile** | Docker container setup |
| **docker-compose.yml** | Docker automation (optional) |

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
docker run -v C:\MyLogs:/log file-accessor:latest

# Linux/Mac
docker run -v /home/user/logs:/log file-accessor:latest
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

All settings are in `appsettings.json`. **No code changes needed!**

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

**Faster execution (no delay):**
```json
"ThreadDelayMilliseconds": 0
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

## ?? How Thread Synchronization Works

### The Problem
```
Thread 1: Wants to write
Thread 2: Wants to write
?
Both try at same time ? FILE CORRUPTION! ?
```

### Our Solution
```csharp
lock (sharedResource)  // Only ONE thread can enter at a time
{
    // File write happens here
    // Other threads must wait their turn
}
```

Each thread acquires the lock, writes its data, then releases it. This ensures clean, ordered writes.

---

## ?? Useful Commands

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

## ?? License

Educational project demonstrating .NET 10 concurrent file access patterns.

