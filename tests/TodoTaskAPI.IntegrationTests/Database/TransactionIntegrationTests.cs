using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using TodoTaskAPI.Application.DTOs;
using Xunit.Abstractions;
using Xunit;
using TodoTaskAPI.Core.Entities;
using TodoTaskAPI.IntegrationTests.Infrastructure;

namespace TodoTaskAPI.IntegrationTests.Database
{
    /// <summary>
    /// Integration tests focusing on database transaction integrity
    /// Verifies that database operations maintain ACID properties under various conditions
    /// </summary>
    public class TransactionIntegrationTests : IClassFixture<PostgreSqlContainerTest>
    {
        private readonly HttpClient _client;
        private readonly ITestOutputHelper _output;
        PostgreSqlContainerTest _fixture;

        public TransactionIntegrationTests(PostgreSqlContainerTest fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _client = fixture.Factory.CreateClient();
            _output = output;
        }

        [Fact]
        public async Task EndToEnd_LargeTransaction_MaintainsConsistency()
        {
            // Najpierw czyścimy bazę danych
            await _fixture.CleanDatabaseAsync();

            // Arrange - Create collection of todos
            var todos = Enumerable.Range(1, 100).Select(i => new CreateTodoDto
            {
                Title = $"Large Transaction Todo {i}",
                Description = $"Part of large transaction test {i}",
                ExpiryDateTime = DateTime.UtcNow.AddDays(i),
                PercentComplete = i % 100
            }).ToList();

            // Act - Add todos to the database
            foreach (var todo in todos)
            {
                var response = await _client.PostAsJsonAsync("/api/todos", todo);
                _output.WriteLine($"Response for Todo '{todo.Title}': {response.StatusCode}");

                // Retry failed requests
                if (!response.IsSuccessStatusCode)
                {
                    await Task.Delay(200);
                    response = await _client.PostAsJsonAsync("/api/todos", todo);
                }

                Assert.True(response.IsSuccessStatusCode, $"Failed to create todo: {todo.Title}");
                await Task.Delay(100);
            }

            // Assert - Verify that all todos were saved
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
