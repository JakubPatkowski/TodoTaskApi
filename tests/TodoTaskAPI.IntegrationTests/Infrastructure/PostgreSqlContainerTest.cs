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

            // Polityka ponownych prób
            var retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(10, // Zwiększona liczba prób
                    retryAttempt => 
                        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // Exponential backoff
                    onRetry: (exception, timeSpan, retryCount, context) =>
                    {
                        Console.WriteLine($"Próba {retryCount}: Oczekiwanie {timeSpan.TotalSeconds} sekund po błędzie: {exception.Message}");
                    });

            await retryPolicy.ExecuteAsync(async () =>
            {
                using var scope = Factory.Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Poczekaj na gotowość bazy danych
                var isReady = false;
                for (int i = 0; i < 30 && !isReady; i++) // Maksymalnie 30 sekund oczekiwania
                {
                    try
                    {
                        await db.Database.CanConnectAsync();
                        isReady = true;
                    }
                    catch
                    {
                        await Task.Delay(1000);
                    }
                }

                if (!isReady)
                {
                    throw new Exception("Nie można połączyć się z bazą danych po 30 sekundach.");
                }

                // Upewnij się, że baza danych jest czysta i zmigrowna
                await db.Database.EnsureDeletedAsync();
                await db.Database.MigrateAsync();

                // Inicjalizacja bez danych testowych
                await DbInitializer.Initialize(db, seedTestData: false);
            });
        }

        public async Task DisposeAsync()
        {
            using var scope = Factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Database.EnsureDeletedAsync();

            await Factory.DisposeAsync();
            await _dbContainer.DisposeAsync();
        }

        // Helper do czyszczenia bazy między testami
        public async Task CleanDatabaseAsync()
        {
            var retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(5,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

            await retryPolicy.ExecuteAsync(async () =>
            {
                using var scope = Factory.Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await db.Database.EnsureDeletedAsync();
                await db.Database.MigrateAsync();
                await DbInitializer.Initialize(db, seedTestData: false);
            });
        }
    }
}
