# WikiRAG - Knowledge Retrieval and Generation API

A modern .NET Core 9 Web API project designed for Wikipedia-based Retrieval-Augmented Generation (RAG) capabilities.

## Project Structure

```
WikiRAG/
├── src/                              # Source code
│   ├── Controllers/                  # API Controllers
│   ├── Data/                        # Data Access Layer
│   ├── Models/                      # Domain Models
│   │   └── DTOs/                    # Data Transfer Objects
│   ├── Services/                    # Business Logic Layer
│   ├── Migrations/                  # Entity Framework Migrations
│   ├── Properties/                  # Project properties and settings
│   ├── bin/                         # Compiled binaries (auto-generated)
│   └── obj/                         # Build artifacts (auto-generated)
├── tests/                           # Test Projects
│   └── WikiRAG.Tests/              # Main test project
│       ├── Services/               # Service layer tests
│       ├── bin/                    # Test binaries (auto-generated)
│       └── obj/                    # Test build artifacts (auto-generated)
├── docs/                           # Documentation
└── ...                             # Configuration files and Docker setup
```

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [PostgreSQL](https://www.postgresql.org/download/) with [pgvector extension](https://github.com/pgvector/pgvector) - Required for vector storage and similarity search
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

3. **Apply database migrations**
   ```bash
   dotnet ef database update
   ```
   *Note: Ensure PostgreSQL with pgvector extension is running and the connection string is configured in appsettings.json*

4. **Run the application**
   ```bash
   dotnet run
   ```

5. **Access the API**
   - HTTP: http://localhost:5254
   - HTTPS: https://localhost:7028
   - Health Check: http://localhost:5254/api/health
   - OpenAPI/Scalar: https://localhost:7028/scalar (Development only)

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

### Document Management
- `GET /api/document` - List all documents with pagination and optional filtering
- `GET /api/document/{id}` - Retrieve a specific document by ID
- `POST /api/document` - Create a new document in the knowledge base
- `PUT /api/document/{id}` - Update an existing document
- `DELETE /api/document/{id}` - Delete a document from the knowledge base
- `POST /api/document/bulk` - Bulk upload multiple documents

### Document Chunking
- `POST /api/chunking/{documentId}/process` - Process a document into chunks using specified strategy
- `GET /api/chunking/{documentId}/chunks` - Get all chunks for a document
- `POST /api/chunking/{documentId}/preview` - Preview how a document would be chunked (without saving)

### Database Testing (Development)
- `GET /api/databasetest/connection` - Test database connection
- `GET /api/databasetest/tables` - Verify database tables and get counts
- `POST /api/databasetest/test-document` - Create a test document for verification

## Testing

The project includes a comprehensive test suite to ensure code quality and reliability.

### Test Structure

```
tests/
└── WikiRAG.Tests/           # Main test project
    ├── Services/            # Service layer tests
    │   └── ChunkingServiceTests.cs
    └── WikiRAG.Tests.csproj # Test project configuration
```

### Test Framework

- **Testing Framework**: xUnit
- **In-Memory Database**: Entity Framework Core InMemory provider for isolated testing
- **Code Coverage**: Coverlet for test coverage analysis
- **Test Runner**: Visual Studio Test Platform

### Running Tests

#### Command Line
```bash
# Navigate to the test project
cd tests/WikiRAG.Tests

# Run all tests
dotnet test

# Run tests with detailed output
dotnet test --verbosity normal

# Run tests with code coverage
dotnet test --collect:"XPlat Code Coverage"
```

#### Visual Studio
- Open the solution in Visual Studio
- Use Test Explorer to run and debug tests
- Right-click on test methods to run individual tests

#### Docker Testing
```bash
# Run tests in Docker container
docker run --rm -v ${PWD}:/app -w /app/tests/WikiRAG.Tests mcr.microsoft.com/dotnet/sdk:9.0 dotnet test
```

### Test Categories

#### Service Tests
- **ChunkingServiceTests**: Tests for document chunking functionality
  - Header-based chunking with hierarchy preservation
  - Sentence-based chunking with overlap
  - Fixed-size chunking with word boundaries
  - Content preservation (code blocks, tables, lists)
  - Metadata generation and validation

#### Integration Tests
- Database connectivity and operations
- Controller endpoint functionality
- End-to-end API workflows

### Writing Tests

When adding new features, ensure to:

1. **Unit Tests**: Test individual service methods and business logic
2. **Integration Tests**: Test API endpoints and database interactions
3. **Test Coverage**: Aim for high test coverage on critical paths
4. **Mock Dependencies**: Use in-memory database for isolation
5. **Test Data**: Create realistic test scenarios and edge cases

### Test Configuration

The test project uses:
- **In-Memory Database**: Each test gets a fresh database instance
- **xUnit Facts**: For simple test cases
- **xUnit Theories**: For parameterized tests with multiple data sets
- **IDisposable**: Proper cleanup of test resources

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
