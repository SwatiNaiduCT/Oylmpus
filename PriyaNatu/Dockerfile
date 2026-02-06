# ---------- Build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /app

COPY . .
RUN dotnet publish -c Release -o out

# ---------- Runtime stage ----------
FROM mcr.microsoft.com/dotnet/runtime:6.0
WORKDIR /app

# Copy compiled app
COPY --from=build /app/out .

# Create directory for file output
RUN mkdir -p /log

# Default environment variables (can be overridden at runtime)
ENV AppConfig__OutputFilePath=/log/out.txt
ENV AppConfig__ThreadCount=10
ENV AppConfig__WritesPerThread=10

# Allow file access via volume
VOLUME ["/log"]

ENTRYPOINT ["dotnet", "Tasklog.dll"]