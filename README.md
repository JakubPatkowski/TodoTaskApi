# Todo Task API

A REST API for managing todo tasks with support for pagination and rate limiting.

## Features

- Get all todos with pagination
- Get specific todo by ID or title
- Get upcoming todos (today/next day/current week/custom)
- Create new todos
- Update existing todos
- Update completion percentage
- Mark todos as done
- Delete todos
- Rate limiting using token bucket algorithm
- PostgreSQL database with Entity Framework Core
- Comprehensive test coverage
- API documentation with Swagger

## Prerequisites

- [.NET SDK 8.0](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker](https://www.docker.com/products/docker-desktop/)
- Docker Compose

## Quick Start

1. Clone the repository:
   ```bash
   git clone https://github.com/JakubPatkowski/TodoTaskApi
   cd TodoTaskApi
   ```

2. Build and run with Docker Compose:
   ```bash
   docker-compose up --build
   ```

   - The API will be available at [http://localhost:5034](http://localhost:5034).
   - Swagger documentation can be accessed at [http://localhost:5034/swagger](http://localhost:5034/swagger).

## Development Setup

If you want to run the project without Docker:

1. Install PostgreSQL and create a database named TodoDb

2. Update the connection string in `appsettings.Development.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection":" 
         Host=localhost;
         Database=TodoDb;
         Username=postgres;
         Password=postgres"
     }
   }
   ```

3. Run the application:
   ```bash
   dotnet run --project src/TodoTaskAPI.API
   ```

## Database

The application uses EF Core migrations and automatic database initialization. No manual migration steps are required as they are applied automatically on startup.

When the application starts for the first time, it will:

- Create the database if it doesn't exist
- Apply all migrations
- Seed initial test data if the database is empty

## Testing

- Run all tests from solution root
  ```bash
  dotnet test TodoTaskApi.sln
  ```

- Run unit tests:
  ```bash
  dotnet test tests/TodoTaskAPI.UnitTests/TodoTaskAPI.UnitTests.csproj
  ```

- Run integration tests:
  ```bash
  dotnet test tests/TodoTaskAPI.IntegrationTests/TodoTaskAPI.IntegrationTests.csproj
  ```

## Project Structure

- `src/TodoTaskAPI.API` - Web API layer
- `src/TodoTaskAPI.Application` - Application services and DTOs
- `src/TodoTaskAPI.Core` - Domain entities and interfaces
- `src/TodoTaskAPI.Infrastructure` - Database and repositories
- `tests/TodoTaskAPI.UnitTests` - Unit tests
- `tests/TodoTaskAPI.IntegrationTests` - Integration tests

## License

This project is licensed under the MIT License - see the LICENSE file for details.
