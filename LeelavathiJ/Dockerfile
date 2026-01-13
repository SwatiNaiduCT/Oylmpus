
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

# Copy project file for restore caching
COPY ["ThreadWriter.csproj", "ThreadWriter/"]
RUN dotnet restore "ThreadWriter/ThreadWriter.csproj"

# Copy the rest of the source
COPY . .
WORKDIR /src/ThreadWriter

# Publish
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ThreadWriter.dll"]
