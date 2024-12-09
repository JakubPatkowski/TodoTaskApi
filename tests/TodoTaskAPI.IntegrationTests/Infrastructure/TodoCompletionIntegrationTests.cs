using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using TodoTaskAPI.Application.DTOs;
using Xunit.Abstractions;
using Xunit;
using System.Net;

namespace TodoTaskAPI.IntegrationTests.Infrastructure
{
    /// <summary>
    /// Integration tests for todo completion endpoints
    /// Tests the complete flow through all layers using real database
    /// </summary>
    public class TodoCompletionIntegrationTests : IClassFixture<PostgreSqlContainerTest>
    {
        private readonly PostgreSqlContainerTest _fixture;
        private readonly HttpClient _client;
        private readonly ITestOutputHelper _output;

        public TodoCompletionIntegrationTests(PostgreSqlContainerTest fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _client = fixture.Factory.CreateClient();
            _output = output;
        }

        /// <summary>
        /// Verifies completion percentage update works through the entire stack
        /// Tests the complete flow of updating completion percentage
        /// </summary>
        [Theory]
        [InlineData(50, false)]  // Regular update
        [InlineData(100, true)]  // Should mark as done
        public async Task UpdateCompletion_EndToEnd_UpdatesCorrectly(int percentage, bool shouldBeDone)
        {
            // Arrange
            var todo = await CreateTestTodo("Completion Test Todo");
            var updateDto = new UpdateTodoCompletionDto
            {
                PercentComplete = percentage
            };

            // Act
            var response = await _client.PatchAsync(
                $"api/todos/{todo.Id}/completion",
                JsonContent.Create(updateDto));

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content
                .ReadFromJsonAsync<ApiResponseDto<TodoDto>>();
            Assert.NotNull(result?.Data);
            Assert.Equal(percentage, result.Data.PercentComplete);
            Assert.Equal(shouldBeDone, result.Data.IsDone);

            // Verify persistence
            var getTodoResponse = await _client.GetAsync($"api/todos/search?id={todo.Id}");
            var getTodoResult = await getTodoResponse.Content
                .ReadFromJsonAsync<ApiResponseDto<IEnumerable<TodoDto>>>();
            var persistedTodo = getTodoResult?.Data?.FirstOrDefault();
            Assert.NotNull(persistedTodo);
            Assert.Equal(percentage, persistedTodo.PercentComplete);
            Assert.Equal(shouldBeDone, persistedTodo.IsDone);
        }

        /// <summary>
        /// Verifies done status update works through the entire stack
        /// Tests the complete flow of updating done status
        /// </summary>
        [Theory]
        [InlineData(true, 100)]   // Mark as done
        [InlineData(false, 0)]    // Mark as not done
        public async Task UpdateDoneStatus_EndToEnd_UpdatesCorrectly(bool isDone, int expectedPercentage)
        {
            // Arrange
            var todo = await CreateTestTodo("Done Status Test Todo");
            var updateDto = new UpdateTodoDoneStatusDto
            {
                IsDone = isDone
            };

            // Act
            var response = await _client.PatchAsync(
                $"api/todos/{todo.Id}/done",
                JsonContent.Create(updateDto));

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content
                .ReadFromJsonAsync<ApiResponseDto<TodoDto>>();
            Assert.NotNull(result?.Data);
            Assert.Equal(isDone, result.Data.IsDone);
            Assert.Equal(expectedPercentage, result.Data.PercentComplete);

            // Verify persistence
            var getTodoResponse = await _client.GetAsync($"api/todos/search?id={todo.Id}");
            var getTodoResult = await getTodoResponse.Content
                .ReadFromJsonAsync<ApiResponseDto<IEnumerable<TodoDto>>>();
            var persistedTodo = getTodoResult?.Data?.FirstOrDefault();
            Assert.NotNull(persistedTodo);
            Assert.Equal(isDone, persistedTodo.IsDone);
            Assert.Equal(expectedPercentage, persistedTodo.PercentComplete);
        }

        /// <summary>
        /// Verifies validation works correctly through the entire stack
        /// Tests input validation in real environment
        /// </summary>
        [Theory]
        [InlineData(-1)]
        [InlineData(101)]
        public async Task UpdateCompletion_EndToEnd_ValidatesInput(int invalidPercentage)
        {
            // Arrange
            var todo = await CreateTestTodo("Validation Test Todo");
            var updateDto = new UpdateTodoCompletionDto { PercentComplete = invalidPercentage };

            // Act
            var response = await _client.PatchAsync(
                $"api/todos/{todo.Id}/completion",
                JsonContent.Create(updateDto));

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            _output.WriteLine($"Response content: {content}");

            var errorResponse = await response.Content.ReadFromJsonAsync<ProblemDetails>();
            Assert.NotNull(errorResponse);
            Assert.Contains("PercentComplete", content);
        }

        public class ProblemDetails
        {
            public string? Type { get; set; }
            public string? Title { get; set; }
            public int Status { get; set; }
            public Dictionary<string, string[]>? Errors { get; set; }
        }

        /// <summary>
        /// Verifies concurrent updates are handled correctly
        /// Tests system behavior under concurrent load
        /// </summary>
        [Fact]
        public async Task UpdateCompletion_ConcurrentRequests_HandlesCorrectly()
        {
            // Arrange
            var todo = await CreateTestTodo("Concurrency Test Todo");
            var percentages = new[] { 25, 50, 75, 100 };
            var tasks = percentages.Select(p =>
                _client.PatchAsync(
                    $"api/todos/{todo.Id}/completion",
                    JsonContent.Create(new UpdateTodoCompletionDto { PercentComplete = p })))
                .ToList();

            // Act
            var responses = await Task.WhenAll(tasks);

            // Assert
            Assert.All(responses, r =>
                Assert.True(r.IsSuccessStatusCode ||
                    r.StatusCode == System.Net.HttpStatusCode.TooManyRequests));

            // Verify final state
            var getTodoResponse = await _client.GetAsync($"api/todos/search?id={todo.Id}");
            var getTodoResult = await getTodoResponse.Content
                .ReadFromJsonAsync<ApiResponseDto<IEnumerable<TodoDto>>>();
            var finalTodo = getTodoResult?.Data?.FirstOrDefault();
            Assert.NotNull(finalTodo);
            // Final state should be one of the attempted percentages
            Assert.Contains(finalTodo.PercentComplete, percentages);
        }

        private async Task<TodoDto> CreateTestTodo(string title)
        {
            var createDto = new CreateTodoDto
            {
                Title = title,
                Description = "Test Description",
                ExpiryDateTime = DateTime.UtcNow.AddDays(1),
                PercentComplete = 0
            };

            var response = await _client.PostAsJsonAsync("api/todos", createDto);
            var result = await response.Content
                .ReadFromJsonAsync<ApiResponseDto<TodoDto>>();
            return result?.Data ??
                throw new InvalidOperationException("Failed to create test todo");
        }
    }
}
