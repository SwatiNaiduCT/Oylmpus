# Multi-stage Dockerfile 
# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["FileAccessor.csproj", "."]
RUN dotnet restore "./FileAccessor.csproj"

COPY . .
RUN dotnet build "./FileAccessor.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Stage 2: Publish
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./FileAccessor.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app

# Create log directory with proper permissions
RUN mkdir -p /log && chmod 777 /log

# Declare volume for persistent logging
VOLUME ["/log"]

# Copy published application
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "FileAccessor.dll"]
