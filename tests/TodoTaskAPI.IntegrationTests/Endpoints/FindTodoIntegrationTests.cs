using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using TodoTaskAPI.Application.DTOs;
using TodoTaskAPI.Core.Entities;
using TodoTaskAPI.Infrastructure.Data;
using TodoTaskAPI.IntegrationTests.Infrastructure;
using Xunit.Abstractions;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using DotNet.Testcontainers.Builders;

namespace TodoTaskAPI.IntegrationTests.Endpoints
{
    /// <summary>
    /// Integration tests for the Find Todo endpoint
    /// Tests the complete request pipeline including validation, database operations, and response formatting
    /// </summary>
    public class FindTodoIntegrationTests : IClassFixture<PostgreSqlContainerTest>
    {
        private readonly PostgreSqlContainerTest _fixture;
        private readonly ITestOutputHelper _output;
        private readonly HttpClient _client;

        public FindTodoIntegrationTests(PostgreSqlContainerTest fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _output = output;
            _client = fixture.Factory.CreateClient();
        }

        /// <summary>
        /// Verifies that searching by an existing ID returns the correct todo item
        /// </summary>
        [Fact]
        public async Task FindTodos_WithExistingId_ReturnsTodo()
        {
            // Arrange
            var todo = await CreateTestTodo("Test Todo for ID Search");

            // Act
            var response = await _client.GetAsync($"/api/todos/search?id={todo.Id}");
            var content = await response.Content.ReadFromJsonAsync<ApiResponseDto<IEnumerable<TodoDto>>>();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(content?.Data);
            var todos = content.Data.ToList();
            Assert.Single(todos);
            Assert.Equal(todo.Id.ToString(), todos.First().Id.ToString());
        }

        /// <summary>
        /// Verifies that searching by an existing title returns the correct todo item
        /// </summary>
        [Fact]
        public async Task FindTodos_WithExistingTitle_ReturnsTodo()
        {
            // Arrange
            var uniqueTitle = $"Unique Test Title {Guid.NewGuid()}";
            await CreateTestTodo(uniqueTitle);

            // Act
            var response = await _client.GetAsync($"/api/todos/search?title={Uri.EscapeDataString(uniqueTitle)}");
            var content = await response.Content.ReadFromJsonAsync<ApiResponseDto<IEnumerable<TodoDto>>>();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(content?.Data);
            var todos = content.Data.ToList();
            Assert.Single(todos);
            Assert.Equal(uniqueTitle, todos.First().Title);
        }

        /// <summary>
        /// Verifies that searching by a non-existing ID returns a NotFound response
        /// </summary>
        [Fact]
        public async Task FindTodos_WithNonExistingId_ReturnsNotFound()
        {
            // Act
            var response = await _client.GetAsync($"/api/todos/search?id={Guid.NewGuid()}");
            var content = await response.Content.ReadFromJsonAsync<ApiResponseDto<IEnumerable<TodoDto>>>();

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.NotNull(content);
            Assert.NotNull(content.Data);
            Assert.Empty(content.Data.ToList());
        }

        /// <summary>
        /// Verifies that searching with international characters works correctly
        /// </summary>
        [Fact]
        public async Task FindTodos_WithInternationalCharacters_ReturnsTodo()
        {
            // Arrange
            var titleWithSpecialChars = "zażółć gęsią jaźń";
            await CreateTestTodo(titleWithSpecialChars);

            // Act
            var response = await _client.GetAsync($"/api/todos/search?title={Uri.EscapeDataString(titleWithSpecialChars)}");
            var content = await response.Content.ReadFromJsonAsync<ApiResponseDto<IEnumerable<TodoDto>>>();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(content?.Data);
            var todos = content.Data.ToList();
            Assert.Single(todos);
            Assert.Equal(titleWithSpecialChars, todos.First().Title);
        }

        /// <summary>
        /// Verifies that partial title matches return NotFound
        /// </summary>
        [Fact]
        public async Task FindTodos_WithPartialTitleMatch_ReturnsNotFound()
        {
            // Arrange
            var fullTitle = "Complete Test Title";
            await CreateTestTodo(fullTitle);

            // Act
            var response = await _client.GetAsync($"/api/todos/search?title=Complete");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        /// <summary>
        /// Verifies that searching without parameters returns BadRequest
        /// </summary>
        [Fact]
        public async Task FindTodos_WithInvalidParameters_ReturnsBadRequest()
        {
            // Act
            var response = await _client.GetAsync("/api/todos/search");
            var content = await response.Content.ReadFromJsonAsync<ApiResponseDto<ValidationErrorResponse>>();

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.NotNull(content);
            Assert.NotNull(content.Message);
            Assert.Contains("parameter", content.Message.ToLower());
        }

        /// <summary>
        /// Tests various title formats to verify validation rules
        /// </summary>
        //[Theory]
        //[InlineData("Test Todo 123", true)] // Basic alphanumeric with spaces
        //[InlineData("Zadanie-testowe.txt", true)] // With allowed special characters
        //[InlineData("Ważne spotkanie!", true)] // With Polish characters
        //[InlineData("Przegląd (kod)", true)] // With parentheses
        //[InlineData("Test: część 1", true)] // With colon
        //[InlineData("<script>alert(1)</script>", false)] // XSS attempt
        //[InlineData("Admin=True--", false)] // SQL injection attempt
        //[InlineData("Test\\File\\Path", false)] // Path traversal attempt
        //public async Task FindTodos_WithVariousTitleFormats_ValidatesCorrectly(string title, bool shouldBeValid)
        //{
        //    // Arrange
        //    _output.WriteLine($"Testing title: {title}");

        //    // Act
        //    var response = await _client.GetAsync($"/api/todos/search?title={Uri.EscapeDataString(title)}");

        //    // Assert
        //    if (shouldBeValid)
        //    {
        //        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        //        var content = await response.Content.ReadFromJsonAsync<ApiResponseDto<IEnumerable<TodoDto>>>();
        //        Assert.NotNull(content);
        //        Assert.Equal(StatusCodes.Status404NotFound, content.StatusCode);
        //    }
        //    else
        //    {
        //        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        //        var content = await response.Content.ReadFromJsonAsync<ApiResponseDto<ValidationErrorResponse>>();
        //        Assert.NotNull(content);
        //        Assert.Equal(StatusCodes.Status400BadRequest, content.StatusCode);
        //        await Assert.ThrowsAsync<TodoTaskAPI.Core.Exceptions.ValidationException>(
        //        async () => await _client.GetAsync("/api/todos/search?title="));
        //    }
        //}

        /// <summary>
        /// Tests the endpoint's behavior under high load
        /// </summary>
        [Fact]
        public async Task FindTodos_UnderHighLoad_HandlesRequestsProperly()
        {
            // Arrange
            var titles = Enumerable.Range(1, 10)
                .Select(i => $"High Load Test Todo {i}")
                .ToList();

            foreach (var title in titles)
            {
                await CreateTestTodo(title);
            }

            // Act
            var tasks = titles.Select(title =>
                _client.GetAsync($"/api/todos/search?title={Uri.EscapeDataString(title)}"));
            var responses = await Task.WhenAll(tasks);

            // Assert
            Assert.All(responses, response =>
            {
                Assert.True(
                    response.StatusCode == HttpStatusCode.OK ||
                    response.StatusCode == HttpStatusCode.TooManyRequests,
                    $"Unexpected status code: {response.StatusCode}");
            });
        }

        /// <summary>
        /// Helper method to create a test todo item in the database
        /// </summary>
        private async Task<Todo> CreateTestTodo(string title)
        {
            using var scope = _fixture.Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var todo = new Todo
            {
                Id = Guid.NewGuid(),
                Title = title,
                Description = "Test Description",
                ExpiryDateTime = DateTime.UtcNow.AddDays(1),
                PercentComplete = 0,
                CreatedAt = DateTime.UtcNow,
                IsDone = false
            };

            context.Todos.Add(todo);
            await context.SaveChangesAsync();

            _output.WriteLine($"Created test todo with ID: {todo.Id} and Title: {todo.Title}");
            return todo;
        }
    }
}
