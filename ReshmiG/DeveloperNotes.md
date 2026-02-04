Developer Notes - MultiThreadedFileWriter

Overview
- This is a console application that demonstrates synchronized access to a single file from multiple background threads.
- The application targets .NET 8 in the current project file and the Dockerfile and runtime usage have been updated to .NET 8.

Behavior
1. The application writes an initial line `0, 0, <timestamp>` to `/log/out.txt` (timestamp format `HH:mm:ss.fff`).
2. It launches 10 background tasks. Each task appends 10 lines to the same file. Each append is synchronized using a private lock in `FileLogger`.
3. A global `Interlocked.Increment` counter ensures strictly incrementing line numbers across all threads.
4. Background exceptions are collected into a `ConcurrentBag<Exception>` and reported once all tasks complete.
5. The app waits for any key press before exit.

Error handling and considerations
- Directory creation: `FileLogger` ensures the target directory exists using `Directory.CreateDirectory`. If this fails (permission error, invalid path), the constructor will throw and the top-level try/catch will report the error and exit.
- File write errors: `AppendLine` and `InitializeFirstLine` do not swallow exceptions; background tasks capture exceptions and report them back to main. This prevents silent failures.
- Background exceptions: Any exception inside a task is caught and stored in a `ConcurrentBag<Exception>`. The main thread enumerates and logs them. This prevents unobserved task exceptions.
- Top-level exceptions: The main method wraps work in a try/catch to capture and report fatal errors and set a non-zero exit code.
- Thread synchronization: File writes are protected by a simple `lock` to prevent interleaving. The line number uses `Interlocked.Increment` for atomic increments.

Docker / Permissions
- The provided `Dockerfile` builds using the .NET 8 SDK and runs on .NET 8 runtime. The image creates a dedicated unprivileged user and a `/log` directory, and sets `LOG_DIR=/log` by default.
- To run with Docker Engine on Linux (Docker Desktop is not required):
  1. Build the image: `docker build -t multithreadedfilewriter .`
  2. Run: `docker run --rm multithreadedfilewriter`
  3. To persist logs on the host, mount a host directory: `docker run --rm -v /host/log:/log multithreadedfilewriter`

Notes:
- The image runs as an unprivileged `appuser` inside the container for improved security.
- Docker Desktop is not required for Linux Docker Engine. The Visual Studio container tooling requires Docker Desktop on Windows, but you can use the Docker CLI/engine directly to build and run these images on Linux.

Notes on .NET versions
- The project currently targets .NET 8 (see `MultiThreadedFileWriter.csproj`). The Dockerfile and runtime usage are updated to .NET 8 for consistency.

Potential improvements
- Use a dedicated logging library with asynchronous batching (e.g., Serilog with file sink) for higher throughput.
- Use a named Mutex for multi-process synchronization if multiple processes need to write to the same file.
- Add cancellation support to gracefully stop worker tasks.
