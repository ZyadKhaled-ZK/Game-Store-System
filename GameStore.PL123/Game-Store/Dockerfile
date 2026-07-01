FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore with project files only (layer caching)
COPY GameStore.DAL/GameStore.DAL.csproj GameStore.DAL/
COPY GameStore.BLL/GameStore.BLL.csproj GameStore.BLL/
COPY GameStore.Tests/GameStore.Tests.csproj GameStore.Tests/
COPY Game-Store/GameStore.PL.csproj Game-Store/
RUN dotnet restore Game-Store/GameStore.PL.csproj

# Copy everything and build
COPY . .
RUN dotnet publish Game-Store/GameStore.PL.csproj -c Release -o /app/publish \
    --no-restore \
    -p:DebugSymbols=false \
    -p:DebugType=none

# Test stage
FROM build AS test
RUN dotnet test GameStore.Tests/GameStore.Tests.csproj -c Release --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install locales for SQL Server collation support
RUN apt-get update && apt-get install -y locales && rm -rf /var/lib/apt/lists/* && \
    echo "en_US.UTF-8 UTF-8" > /etc/locale.gen && locale-gen

EXPOSE 8080
EXPOSE 8081

COPY --from=build /app/publish .

# Remove debug artifacts (source maps, non-minified libs, license files)
RUN find wwwroot -type f \( -name '*.map' -o -name '*.txt' -o -name 'LICENSE' -o -name 'LICENSE.txt' \) -delete && \
    find wwwroot/lib -type f -name '*.js' ! -name '*.min.js' -delete && \
    find wwwroot/lib -type f -name '*.css' ! -name '*.min.css' -delete

ENTRYPOINT ["dotnet", "GameStore.PL.dll"]
