================================================================================
                    FILE ACCESSOR - QUICK START GUIDE
================================================================================

ABOUT THE APP

A simple console program that creates 10 threads writing to 1 file safely with each thread writing to it 10 times concurrently.
Intention is to do total of 100 writes without data corruption.


HOW TO RUN LOCALLY

1. Open Command Prompt or Terminal
2. Navigate to project folder
3. Type: dotnet run
4. Check file: /log/out.txt


HOW TO RUN IN DOCKER

Navigate to the docker file folder and execute below commands
  docker build -t fileaccessor .
  docker run -v file-logs:/log file-accessor:latest

Local Folder Mount
  docker run -v C:\MyLogs:/log file-accessor:latest
  (Windows: C:\MyLogs  or  Linux: /home/user/logs)


OUTPUT FILE FORMAT

Each line contains content like below
  WriteNumber, ThreadId, Timestamp

Example:
  1, 1, 14:23:45.123
  2, 2, 14:23:45.128
  3, 3, 14:23:45.133
  ...
  100, 10, 14:23:50.456


APPLICATION Files

Program.cs                  - Main app logic (creates 10 threads)
FileHandler/
  FileAccessHandler.cs      - Handles file writing safely
Dockerfile                  - Docker container setup
docker-compose.yml          - Docker automation (optional)
README.md                   - Detailed documentation


FEATURES HANDLED

Thread-Safe    - Lock prevents file corruption
Concurrent     - 10 threads run at same time
Handles Errors - Catches problems gracefully
Containerized  - Runs in Docker easily
OOP Design     - Clean, organized code




CONFIGURATION
All settings are in appsettings.json 

1. Run 50 threads with 20 writes each (1000 total):
   "TotalNumberOfAllowedThreads": 50
   "TotalNumberOfAllowedWritesPerThread": 20

2. Change output path:
   "FilePath": "C:\\MyLogs\\output.txt"

3. Faster execution (no delay):
   "ThreadDelayMilliseconds": 0

4. Detailed timestamps:
   "DatetimeFormat": "yyyy-MM-dd HH:mm:ss.fff"

5. Increase Thread Count (1000 threads):
   "TotalNumberOfAllowedThreads": 1000

USEFUL COMMANDS

# Check if .NET 10 installed
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


================================================================================
