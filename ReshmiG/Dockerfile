# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["MultiThreadedFileWriter.csproj", "."]
RUN dotnet restore "./MultiThreadedFileWriter.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "./MultiThreadedFileWriter.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./MultiThreadedFileWriter.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app

# create /log directory and an unprivileged user to run the app (suitable for Docker Engine on Linux hosts)
RUN mkdir -p /log \
    && groupadd -g 1000 appgroup \
    && useradd -u 1000 -g appgroup -m appuser \
    && chown -R appuser:appgroup /log /app || true

# Expose a LOG_DIR environment variable so the app picks it up by default
ENV LOG_DIR=/log

COPY --from=publish /app/publish .

# Ensure files under /app are owned by the app user
RUN chown -R appuser:appgroup /app || true

# Switch to unprivileged user
USER appuser

ENTRYPOINT ["dotnet", "MultiThreadedFileWriter.dll"]
