# Build
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app /p:UseAppHost=false

# Runs
FROM mcr.microsoft.com/dotnet/runtime:6.0
WORKDIR /app
VOLUME ["/log"]
COPY --from=build /app .
ENTRYPOINT ["dotnet", "ThreadedFileWriter.dll"]