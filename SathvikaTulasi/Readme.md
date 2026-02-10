## Developer Technical Notes

### 1) Concurrency & Ordering
- A single `StreamWriter` is shared by all threads.
- We ensure **strict in-file ordering** by performing **both the global line increment and the file write inside the same `lock`**. This eliminates interleaving and guarantees `<line_count>` appears in ascending order without inversions.
- For the global counter, we use `++_lineCounter` **inside the lock** (no need for `Interlocked` since the lock provides mutual exclusion).

### 2) Time Handling (Testability & Timezone Safety)
- Direct `DateTime.Now` is avoided for testability and timezone neutrality.
- We inject `ITimeProvider` and use **UTC** (`DateTime.UtcNow`) consistently.
- Formatting uses `HH:mm:ss.fff` with `InvariantCulture`.
## Key Files

### `src/Program.cs` (high level)
- Defines `ITimeProvider` and `SystemTimeProvider` (UTC time).
- `LogFile` ensures strict ordering by holding a lock while incrementing and writing.
- Spawns 10 threads via `LogWorker`, each writing 10 times.
- Robust error handling and safe shutdown.

### `Dockerfile`
```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY src/ThreadSafeLogger.csproj ./src/
RUN dotnet restore ./src/ThreadSafeLogger.csproj
COPY ./src ./src
RUN dotnet publish ./src/ThreadSafeLogger.csproj -c Release -o /app/publish /p:UseAppHost=false


