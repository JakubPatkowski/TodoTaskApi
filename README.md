
# Todo Task API

A REST API for managing todo tasks with support for pagination and rate limiting.

## Features

- Get all todos with pagination
- Rate limiting using the token bucket algorithm
- PostgreSQL database with Entity Framework Core
- Docker support
- Comprehensive test coverage
- API documentation with Swagger

## Prerequisites

- .NET 8.0
- PostgreSQL
- Docker (optional)

## Running the Application

### Using Docker

```bash
docker-compose up
```

### Local Development

1. Update the connection string in `appsettings.Development.json`.
2. Run the application:

```bash
dotnet restore
dotnet run --project src/TodoTaskAPI.API
```

## Running Tests

```bash
dotnet test
```

## API Documentation

Swagger documentation is available at `/swagger` when running the application.

## Architecture

The solution follows Clean Architecture principles:

- **API Layer:** REST API endpoints, middleware, configuration
- **Application Layer:** DTOs, services, business logic
- **Core Layer:** Entities, interfaces, domain logic
- **Infrastructure Layer:** Database context, repositories, migrations

## Technologies

- ASP.NET Core 8.0
- Entity Framework Core
- PostgreSQL
- xUnit
- Docker
- Swagger/OpenAPI

