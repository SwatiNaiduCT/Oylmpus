# Use the .NET 6 SDK to build the application
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /app

# Copy the project files into the container
COPY . ./

# Publish the application to the /out directory
RUN dotnet publish -c Release -o /out

# Use the .NET 6 runtime image for the final container
FROM mcr.microsoft.com/dotnet/runtime:6.0
WORKDIR /app

# Copy the published files from the build stage
COPY --from=build /out .

# Set the entry point to run the application
ENTRYPOINT ["dotnet", "MultithreadedFileWriter.dll"]