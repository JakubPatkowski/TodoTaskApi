using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using TodoTaskAPI.Application.DTOs;
using Xunit.Abstractions;
using Xunit;
using TodoTaskAPI.IntegrationTests.Infrastructure;

namespace TodoTaskAPI.IntegrationTests.Api
{
    /// <summary>
    /// Tests the end-to-end functionality of the Todo API endpoints.
    /// Verifies CRUD operations, data persistence, and error handling using a real PostgreSQL database.
    /// </summary>
    public class TodoApiIntegrationTests : IClassFixture<PostgreSqlContainerTest>
    {
        private readonly HttpClient _client;
        private readonly ITestOutputHelper _output;
        private readonly PostgreSqlContainerTest _fixture;

        public TodoApiIntegrationTests(PostgreSqlContainerTest fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _client = fixture.Factory.CreateClient();
            _output = output;
        }


        /// <summary>
        /// Verifies that creating and retrieving a Todo works correctly with proper data persistence.
        /// Tests the full request pipeline including validation, database operations, and response mapping.
        /// </summary>
        [Fact]
        public async Task EndToEnd_CreateAndRetrieveTodo_WorksCorrectly()
        {
            // Arrange
            var newTodo = new CreateTodoDto
            {
                Title = "Integration Test Todo",
                Description = "Testing complete flow",
                ExpiryDateTime = DateTime.UtcNow.AddDays(1),
                PercentComplete = 0
            };

            // Act - Create Todo
            var createResponse = await _client.PostAsJsonAsync("/api/todos", newTodo);
            var createContent = await createResponse.Content.ReadAsStringAsync();
            _output.WriteLine($"Create Response: {createContent}");

            var createdTodo = await createResponse.Content.ReadFromJsonAsync<ApiResponseDto<TodoDto>>();
            Assert.NotNull(createdTodo?.Data);

            // Act - Retrieve Todo
            var getResponse = await _client.GetAsync("/api/todos");
            var getAllContent = await getResponse.Content.ReadFromJsonAsync<ApiResponseDto<IEnumerable<TodoDto>>>();

            // Assert
            Assert.NotNull(getAllContent?.Data);
            var retrievedTodo = getAllContent.Data.FirstOrDefault(t => t.Id == createdTodo.Data.Id);
            Assert.NotNull(retrievedTodo);
            Assert.Equal(newTodo.Title, retrievedTodo.Title);
        }

        [Fact]
        public async Task EndToEnd_PaginationWithRealData_WorksCorrectly()
        {
            // Arrange - Create multiple todos
            var todos = Enumerable.Range(1, 25).Select(i => new CreateTodoDto
            {
                Title = $"Pagination Todo {i}",
                ExpiryDateTime = DateTime.UtcNow.AddDays(i),
                PercentComplete = 0
            });

            foreach (var todo in todos)
            {
                await _client.PostAsJsonAsync("/api/todos", todo);
            }

            // Act - Get first page
            var response = await _client.GetAsync("/api/todos?pageNumber=1&pageSize=10");
            var content = await response.Content.ReadFromJsonAsync<ApiResponseDto<PaginatedResponseDto<TodoDto>>>();

            // Assert
            Assert.NotNull(content?.Data);
            Assert.Equal(10, content.Data.Items.Count());
            Assert.True(content.Data.HasNextPage);
            Assert.False(content.Data.HasPreviousPage);
        }

        [Fact]
        public async Task EndToEnd_ConcurrentRequests_HandleLoadCorrectly()
        {
            // Arrange
            var tasks = new List<Task<HttpResponseMessage>>();
            var newTodo = new CreateTodoDto
            {
                Title = "Concurrent Test Todo",
                ExpiryDateTime = DateTime.UtcNow.AddDays(1),
                PercentComplete = 0
            };

            // Act - Send multiple concurrent requests
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(_client.PostAsJsonAsync("/api/todos", newTodo));
            }

            var responses = await Task.WhenAll(tasks);

            // Assert
            Assert.All(responses, r => Assert.True(r.IsSuccessStatusCode || r.StatusCode == System.Net.HttpStatusCode.TooManyRequests));

            // Verify rate limiting works
            Assert.Contains(responses, r => r.StatusCode == System.Net.HttpStatusCode.TooManyRequests);

            // Verify database consistency
            var getResponse = await _client.GetAsync("/api/todos");
            var content = await getResponse.Content.ReadFromJsonAsync<ApiResponseDto<IEnumerable<TodoDto>>>();
            Assert.NotNull(content?.Data);
        }
    }
}
