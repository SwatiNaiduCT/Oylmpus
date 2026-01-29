# Concurrent File Write Console App

## Overview
This console application demonstrates coordinated, concurrent file writes from multiple tasks to a single output file while preserving the atomicity of combined "counter increment + write" operations.

The application sets up and invokes a `FileWriteWorker` with a configurable number of tasks and writes per task. Each task appends lines to a shared output file in a thread-safe manner, ensuring that every line number corresponds exactly to one write operation.

Each output line follows the format:

```
lineNo, threadId, timestamp
```

---

## Project Structure

```
│
├── Program.cs           # Application entry point; configures and runs the worker
├── FileWriteWorker.cs   # Implements concurrency, synchronization, file I/O, and error handling
└── README.md            # Project documentation
```

---

## Execution Flow

### 1. Startup (`Main`)
- Instantiate `FileWriteWorker`
- Invoke `RunAsync()`

### 2. RunAsync()
- Resolve a user‑writable output path via `GetWritePath()`
- Ensure the output directory exists
- Initialize the output file
- Open a single shared `FileStream` and `StreamWriter` in append mode
- Create **N** worker tasks
- Each task executes `DoWork(sharedWriter)`
- Await `Task.WhenAll(...)`
- Log expected vs. actual line counts
- Dispose of the `Barrier`

### 3. DoWork(sharedWriter)
- Wait for all tasks using `_barrier.SignalAndWait()`
- Loop `writesPerTask` times
- Inside a critical section (`lock`):
  - Increment `_lineCounter`
  - Write a formatted line to the shared output file

Output format:

```
{lineNo}, {threadId}, {HH:mm:ss.fff timestamp}
```

### 4. Shutdown
- Tasks complete execution
- Resources are disposed
- Completion message printed
- Application waits for keypress before exit

---

## Threading & Synchronization

### Concurrency Model
- Tasks are spawned using `Task.Run`
- All tasks share a single `StreamWriter`

### Barrier Synchronization
- A `Barrier` ensures all tasks begin writing simultaneously

### Critical Section
- A `lock` protects:
  - The line counter increment (`++_lineCounter`)
  - The corresponding `WriteLine`

This guarantees one‑to‑one pairing of line numbers and writes.

### Thread Identification
- `Environment.CurrentManagedThreadId` is recorded per line for diagnostic purposes

---

## File I/O Strategy

### Path Resolution
- **Windows**: `%LocalAppData%/out.txt`
- **Non‑Windows**: `/log/out.txt`

### File Initialization
- File is created or truncated at startup
- An initial UTC entry is written:

```
0, 0, HH:mm:ss.fff
```

### Append Mode
- File opened with `FileMode.Append`
- `FileShare.Read` allows concurrent readers
- `StreamWriter.AutoFlush = false` for better performance

---

## Error Handling & Resilience

### Input Validation
- Constructor enforces:
  - `taskCount > 0`
  - `writesPerTask > 0`

### I/O Exceptions
- `UnauthorizedAccessException` handled explicitly
- General exception handling for other I/O failures

### Per‑Task Fault Isolation
- Exceptions in `DoWork` log:
  - Thread ID
  - Current `_lineCounter` value

### Resource Cleanup
- `Barrier` disposed in `finally` block
- `FileStream` and `StreamWriter` disposed via `using`
---
## Running the Application with Docker

You can build and run this console application using the Docker Engine. The following commands assume Docker is installed and running on your system.

### 1. Build the Docker Image

From the root of the project (where the `Dockerfile` is located), run:

```
sudo docker build -t filewritetask:1.0 .
```

This command:
- Uses the local `Dockerfile`
- Builds the image
- Tags it as `filewritetask:1.0`

### 2. Run the Container

After the image is built successfully, execute:

```
sudo docker run --rm -it filewritetask:1.0
```

Explanation of flags:
- `--rm` : Automatically removes the container when it exits
- `-it`  : Runs the container in interactive mode with a terminal attached

The application will execute inside the container, perform concurrent file writes, print summary output, and then terminate.


## Sample Output

```
0, 0, 13:55:12.123
1, 12, 19:21:43.681
2, 9, 19:21:43.682
3, 12, 19:21:43.683
```
