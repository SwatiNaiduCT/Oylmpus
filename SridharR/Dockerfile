# ---------- Build stage ----------
    FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
    WORKDIR /src
    
    # 1) Copy only csproj first for better restore caching
    COPY ["Filewritetask.csproj", "./"]
    RUN dotnet restore "Filewritetask.csproj"
    
    # 2) Copy the remaining source and build
    COPY . .
    
    RUN dotnet publish "Filewritetask.csproj" -c Release -o /app/publish \
        -p:PublishReadyToRun=false -p:TieredPGO=true
    
    # ---------- Runtime stage ----------
    FROM mcr.microsoft.com/dotnet/runtime:6.0 AS final
    # Create a non-root user and a writable /log (your code writes /log/out.txt)
    # 1000 is a common UID/GID for non-root app users
    RUN addgroup --system app && adduser --system --ingroup app --uid 1000 app \
        && mkdir -p /app /log \
        && chown -R app:app /app /log
    
    USER app
    WORKDIR /app
    
    # Copy published bits
    COPY --from=build /app/publish ./
      
    # Ensure /log exists and is writable at runtime (also useful for mounts)
    VOLUME ["/log"]
    
    # If you want the container to be easily overrideable via 'docker run ...'
    # keep ENTRYPOINT and add CMD for arguments (none needed here)
    ENTRYPOINT ["dotnet", "Filewritetask.dll"]
