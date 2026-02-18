# Threaded File Writer

This is a small .NET 6 console application that writes to a single text file using multiple threads.
The idea was to show basic multithreading, safe file writing, and running the program inside a Linux Docker container.

## How it works
- The app creates **out.txt** inside `/log (or C:\junk\out.txt` when running locally on Windows).
- It writes the first line as:
`0, 0, <timestamp>`
- Then it starts **10 threads**. Each thread writes **10 lines** to the file.
Every line looks like:
`<line_number>, <thread_id>, <timestamp>`
- A simple lock is used so that only one thread writes to the file at a time.
This keeps the line numbers in the correct order (1–100).
- If any thread fails, the exception is captured and printed after all threads finish.
- The app waits for Enter before closing so it can also run inside Docker with -i.

## Locks & Synchronization
- Since all threads write to the **same file**, I used a lock to avoid mixed or corrupted output.
- Only one thread can enter the writing code block at a time.
- Inside the lock, the app:
  - Reads the next line number
  - Writes the line
  - Increments the number
 - This makes sure line numbers stay in order even when threads run at the same time.

## Error Handling
- If a thread hits an error, it is caught and added to a shared list.
- After all threads complete, the main program checks if any errors were collected.
- Startup errors (like file permission issues) are also caught and shown.
- The program never crashes silently — it prints the issue, waits for - Enter, and exits cleanly.

## Important files
- **Program.cs**: main entry, thread setup, final checks.
- **FileAppender.cs**: writes to the file; contains the lock so writing stays in order.
- **LineWriterWorker.cs**: what each thread does (10 writes + simple error handling).
## Docker build and run
Build the image:
``` 
docker build -t threaded-file-writer:1.0 .
```
Run the container (Linux mode):
```
docker run -i -v C:\junk:/log threaded-file-writer:1.0
```
This will create:
```
C:\junk\out.txt
```
## Docker Hub
Image available at:
```
premkumarsct/threaded-file-writer:1.0
```
Pull and run:
```
docker pull premkumarsct/threaded-file-writer:1.0
docker run -i -v C:\junk:/log premkumarsct/threaded-file-writer:1.0
```