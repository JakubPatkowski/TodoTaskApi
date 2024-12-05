using DotNet.Testcontainers.Builders;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Testcontainers.PostgreSql;
using TodoTaskAPI.Infrastructure.Data;
using Xunit;

namespace TodoTaskAPI.IntegrationTests.Infrastructure
{
    /// <summary>
    /// Test fixture providing PostgreSQL container for integration tests
    /// Ensures tests run against real database in isolated environment
    /// </summary>
    public class PostgreSqlContainerTest : IAsyncLifetime
    {
        private readonly PostgreSqlContainer _dbContainer;
        public readonly WebApplicationFactory<Program> Factory;

        public PostgreSqlContainerTest()
        {
            _dbContainer = new PostgreSqlBuilder()
                .WithImage("postgres:latest")
                .WithDatabase("TodoDb")
                .WithUsername("postgres")
                .WithPassword("postgres")
                .WithExposedPort(5432)
                .WithPortBinding(5432, true)
                .WithWaitStrategy(Wait.ForUnixContainer()
                    .UntilCommandIsCompleted("pg_isready")
                    .UntilPortIsAvailable(5432))
                .Build();

            Factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureServices(services =>
                    {
                        var descriptor = services.SingleOrDefault(
                            d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

                        if (descriptor != null)
                        {
                            services.Remove(descriptor);
                        }

                        services.AddDbContext<ApplicationDbContext>(options =>
                        {
                            options.UseNpgsql(_dbContainer.GetConnectionString());
                        });
                    });
                });
        }

        public async Task InitializeAsync()
        {
            await _dbContainer.StartAsync();

            // Ensure database is clean and migrated
            using var scope = Factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // First, ensure we drop and recreate the database
            await db.Database.EnsureDeletedAsync();
            await db.Database.MigrateAsync();

            // Initialize without seeding test data
            await DbInitializer.Initialize(db, seedTestData: false);
        }

        public async Task DisposeAsync()
        {
            // Clean up the database
            using var scope = Factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Database.EnsureDeletedAsync();

            await Factory.DisposeAsync();
            await _dbContainer.DisposeAsync();
        }

        // Helper method to clean database between tests if needed
        public async Task CleanDatabaseAsync()
        {
            using var scope = Factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Database.EnsureDeletedAsync();
            await db.Database.MigrateAsync();
            await DbInitializer.Initialize(db, seedTestData: false);
        }
    }
}
