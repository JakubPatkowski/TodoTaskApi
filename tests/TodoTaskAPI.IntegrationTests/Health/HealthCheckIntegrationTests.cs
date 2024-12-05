using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using TodoTaskAPI.Application.DTOs;
using TodoTaskAPI.Infrastructure.Data;
using TodoTaskAPI.IntegrationTests.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace TodoTaskAPI.IntegrationTests.Health
{
    /// <summary>
    /// Integration tests verifying system health and readiness
    /// Tests database connectivity and overall application health endpoints
    /// </summary>
    public class HealthCheckIntegrationTests : IClassFixture<PostgreSqlContainerTest>
    {
        private readonly HttpClient _client;
        private readonly ITestOutputHelper _output;
        private readonly PostgreSqlContainerTest _fixture;

        public HealthCheckIntegrationTests(
            PostgreSqlContainerTest fixture,
            ITestOutputHelper output)
        {
            _fixture = fixture;
            _client = fixture.Factory.CreateClient();
            _output = output;
        }

        [Fact]
        public async Task EndToEnd_DatabaseConnection_IsHealthy()
        {
            try
            {
                using var scope = _fixture.Factory.Services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Test połączenia z bazą
                var canConnect = await context.Database.CanConnectAsync();
                _output.WriteLine($"Can connect to database: {canConnect}");

                if (!canConnect)
                {
                    var connectionString = context.Database.GetDbConnection().ConnectionString;
                    _output.WriteLine($"Connection string: {connectionString}");
                }

                // Dodaj testowe todo przed sprawdzeniem endpointu
                var testTodo = new CreateTodoDto
                {
                    Title = "Test Health Check",
                    Description = "Test Description",
                    ExpiryDateTime = DateTime.UtcNow.AddDays(1),
                    PercentComplete = 0
                };

                var createResponse = await _client.PostAsJsonAsync("/api/todos", testTodo);
                _output.WriteLine($"Create Todo Response: {createResponse.StatusCode}");

                // Test endpointu
                var retryCount = 0;
                const int maxRetries = 3;

                while (retryCount < maxRetries)
                {
                    var response = await _client.GetAsync("/api/todos");  // Zmieniono na pobieranie wszystkich todos bez paginacji
                    var content = await response.Content.ReadAsStringAsync();
                    _output.WriteLine($"Attempt {retryCount + 1} - Status: {response.StatusCode}");
                    _output.WriteLine($"Response content: {content}");

                    if (response.IsSuccessStatusCode)
                    {
                        Assert.True(response.IsSuccessStatusCode);
                        return;
                    }

                    retryCount++;
                    await Task.Delay(1000 * retryCount);
                }

                Assert.Fail("Could not establish healthy database connection after retries");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Test failed with exception: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Tests the application's ability to handle multiple concurrent client connections
        /// </summary>
        [Fact]
        public async Task EndToEnd_MultipleClients_CanAccessSimultaneously()
        {
            // Arrange
            var factory = _fixture.Factory;  // Dodajemy brakujący _fixture
            var clients = Enumerable.Range(0, 5)
                .Select(_ => factory.CreateClient())
                .ToList();

            // Act
            var tasks = clients.Select(client =>
                client.GetAsync("/api/todos"));
            var responses = await Task.WhenAll(tasks);

            // Assert
            Assert.All(responses, r => Assert.True(r.IsSuccessStatusCode));
        }
    }
}
