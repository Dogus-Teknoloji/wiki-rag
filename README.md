# WikiRAG - Knowledge Retrieval and Generation API

A modern .NET Core 9 Web API project designed for Wikipedia-based Retrieval-Augmented Generation (RAG) capabilities.

## Project Structure

```
WikiRAG/
├── src/                          # Source code
│   ├── WikiRAG.csproj           # Project file
│   ├── Program.cs               # Application entry point
│   ├── Dockerfile               # Container configuration
│   └── Properties/
│       └── launchSettings.json  # Development settings
├── docs/                        # Documentation
├── docker-compose.yml           # Docker Compose configuration
├── docker-compose.override.yml  # Development overrides
└── README.md                    # This file
```

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker](https://www.docker.com/get-started) (optional, for containerization)
- [Docker Compose](https://docs.docker.com/compose/install/) (optional, for local development)

## Getting Started

### Local Development

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd WikiRAG
   ```

2. **Restore dependencies**
   ```bash
   cd src
   dotnet restore
   ```

3. **Run the application**
   ```bash
   dotnet run
   ```

4. **Access the API**
   - HTTP: http://localhost:5254
   - HTTPS: https://localhost:7028
   - Health Check: http://localhost:5254/api/health
   - OpenAPI/Swagger: https://localhost:7028/swagger (Development only)

### Docker Development

1. **Build and run with Docker Compose**
   ```bash
   docker-compose up --build
   ```

2. **Access the containerized API**
   - HTTP: http://localhost:5000
   - HTTPS: https://localhost:5001
   - Health Check: http://localhost:5000/api/health

### Production Deployment

1. **Build the Docker image**
   ```bash
   docker build -t wikirag-api ./src
   ```

2. **Run the container**
   ```bash
   docker run -p 8080:8080 -p 8081:8081 wikirag-api
   ```

## API Endpoints

### Health Checks
- `GET /api/health` - Application health status

### Sample Endpoints
- `GET /weatherforecast` - Sample weather forecast data (development)

## Environment Configuration

The application supports different environments through configuration:

- **Development**: Full logging, CORS enabled, OpenAPI documentation
- **Production**: Optimized for performance and security

### Environment Variables

| Variable | Description | Default |
|---------|-------------|---------|
| `ASPNETCORE_ENVIRONMENT` | Application environment | `Production` |
| `ASPNETCORE_URLS` | Binding URLs | `https://+:8081;http://+:8080` |

## Docker Configuration

### Multi-stage Build
The Dockerfile uses a multi-stage build process:
1. **Base**: Runtime environment with security hardening
2. **Build**: SDK environment for compilation
3. **Publish**: Creates optimized deployment artifacts
4. **Final**: Production-ready container

### Health Checks
The container includes health checks that verify the application is responding:
- Endpoint: `/api/health`
- Interval: 30 seconds
- Timeout: 3 seconds
- Retries: 3

## Development Guidelines

### Project Architecture
- **Minimal API**: Uses .NET 9's minimal API approach for lightweight endpoints
- **Health Checks**: Built-in health monitoring
- **OpenAPI**: Automatic API documentation generation
- **CORS**: Cross-origin resource sharing for development

### Security Features
- Non-root user execution in containers
- HTTPS redirection
- Security headers (to be implemented)

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Test locally and with Docker
5. Submit a pull request

## License

[Add your license information here]

## Additional Resources

- [.NET 9 Documentation](https://docs.microsoft.com/en-us/dotnet/core/)
- [ASP.NET Core Documentation](https://docs.microsoft.com/en-us/aspnet/core/)
- [Docker Documentation](https://docs.docker.com/)
