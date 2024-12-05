using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using TodoTaskAPI.Application.DTOs;
using Xunit.Abstractions;
using Xunit;
using Polly;
using Polly.Retry;
using TodoTaskAPI.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using TodoTaskAPI.IntegrationTests.Infrastructure;
using System.Net;


namespace TodoTaskAPI.IntegrationTests.Database
{
    /// <summary>
    /// Integration tests verifying database consistency under various load scenarios
    /// </summary>
    public class DatabaseConsistencyIntegrationTests : IClassFixture<PostgreSqlContainerTest>
    {
        private readonly HttpClient _client;
        private readonly ITestOutputHelper _output;
        private readonly PostgreSqlContainerTest _fixture;
        private readonly AsyncRetryPolicy _retryPolicy;

        public DatabaseConsistencyIntegrationTests(
            PostgreSqlContainerTest fixture,
            ITestOutputHelper output)
        {
            _fixture = fixture;
            _client = fixture.Factory.CreateClient();
            _output = output;
            _retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

        }

        [Fact]
        public async Task EndToEnd_ParallelWrites_MaintainsDatabaseConsistency()
        {
            // Reset bazy przed testem
            using (var scope = _fixture.Factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await context.Database.EnsureDeletedAsync();
                await context.Database.EnsureCreatedAsync();
                _output.WriteLine("Database reset completed");
            }

            var todos = Enumerable.Range(1, 20).Select(i => new CreateTodoDto
            {
                Title = $"Parallel Todo {Guid.NewGuid()}",
                ExpiryDateTime = DateTime.UtcNow.AddDays(i),
                PercentComplete = i % 100
            }).ToList();

            // Wykonaj zapis w małych grupach
            await _retryPolicy.ExecuteAsync(async () =>
            {
                var batch = todos.Chunk(5);
                foreach (var items in batch)
                {
                    var createTasks = items.Select(async todo =>
                    {
                        var response = await _client.PostAsJsonAsync("/api/todos", todo);
                        if (response.StatusCode == HttpStatusCode.TooManyRequests)
                        {
                            await Task.Delay(1000); // Wait for rate limit reset
                            response = await _client.PostAsJsonAsync("/api/todos", todo);
                        }
                        return response;
                    });
                    var results = await Task.WhenAll(createTasks);
                    // Log results...
                    await Task.Delay(100);
                }
            });

            // Weryfikacja
            var allTodos = await GetAllTodos();
            _output.WriteLine($"Total todos found: {allTodos.Count}");
            Assert.Equal(todos.Count, allTodos.Count);
        }

        private async Task<List<TodoDto>> GetAllTodos()
        {
            var allTodos = new List<TodoDto>();
            var pageNumber = 1;
            const int pageSize = 50;

            while (true)
            {
                var response = await _client.GetAsync($"/api/todos?pageNumber={pageNumber}&pageSize={pageSize}");
                var content = await response.Content.ReadFromJsonAsync<ApiResponseDto<PaginatedResponseDto<TodoDto>>>();

                if (content?.Data?.Items == null || !content.Data.Items.Any())
                    break;

                allTodos.AddRange(content.Data.Items);
                if (!content.Data.HasNextPage)
                    break;

                pageNumber++;
            }

            return allTodos;
        }

    }
}
