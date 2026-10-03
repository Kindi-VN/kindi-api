# Dockerfile for Kindi.API.WebApi
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["Kindi.API.slnx", "."]
COPY ["src/Kindi.API.Domain/Kindi.API.Domain.csproj", "src/Kindi.API.Domain/"]
COPY ["src/Kindi.API.Application/Kindi.API.Application.csproj", "src/Kindi.API.Application/"]
COPY ["src/Kindi.API.Shared/Kindi.API.Shared.csproj", "src/Kindi.API.Shared/"]
COPY ["src/Kindi.API.Infrastructure/Kindi.API.Infrastructure.csproj", "src/Kindi.API.Infrastructure/"]
COPY ["src/Kindi.API.WebApi/Kindi.API.WebApi.csproj", "src/Kindi.API.WebApi/"]
COPY ["docs/Kindi.API.Documentation/Kindi.API.Documentation.csproj", "docs/Kindi.API.Documentation/"]
COPY ["tests/Kindi.API.UnitTests/Kindi.API.UnitTests.csproj", "tests/Kindi.API.UnitTests/"]
# COPY ["tests/Kindi.API.IntegrationTests/Kindi.API.IntegrationTests.csproj", "tests/Kindi.API.IntegrationTests/"]

# Copy package management files
COPY ["Directory.Packages.props", "."]
COPY ["Directory.Build.props", "."]

# Restore dependencies
RUN dotnet restore Kindi.API.slnx

# Copy all source code
COPY src/ src/
COPY docs/ docs/
COPY tests/ tests/

# Publish the WebApi project
# PublishReadyToRun pre-compiles IL to native code at build time, reducing JIT warmup
# (faster cold start on Render free tier). linux-x64 matches the Debian runtime image.
RUN dotnet publish src/Kindi.API.WebApi/Kindi.API.WebApi.csproj \
    -c Release \
    -r linux-x64 \
    -p:PublishReadyToRun=true \
    -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published files
COPY --from=build /app/publish .

# Copy entrypoint script for Render PORT binding
COPY docker-entrypoint.sh .
RUN chmod +x docker-entrypoint.sh

# Create logs directory
RUN mkdir -p /app/logs

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_USE_POLLING_FILE_WATCHER=1

# Expose port (Render injects PORT at runtime)
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD sh -c 'curl -f http://localhost:${PORT:-8080}/health || exit 1'

# Run the application
ENTRYPOINT ["./docker-entrypoint.sh"]
