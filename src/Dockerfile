# Multi-stage Dockerfile for WikiRAG .NET 9 API
# Optimized for production deployment with security best practices

# Base runtime stage - runs the application
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Create non-root user for security
RUN adduser --disabled-password --gecos '' --uid 1001 appuser && chown -R appuser /app
USER appuser

# Build stage - compiles the application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project file and restore dependencies (cached layer)
COPY ["WikiRAG.csproj", "./"]
RUN dotnet restore "WikiRAG.csproj"

# Copy source code and build
COPY . .
RUN dotnet build "WikiRAG.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish stage - creates deployment artifacts
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "WikiRAG.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final production stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Add health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/api/health || exit 1

ENTRYPOINT ["dotnet", "WikiRAG.dll"]