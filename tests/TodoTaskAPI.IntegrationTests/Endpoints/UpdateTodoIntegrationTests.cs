using System;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TodoTaskAPI.Application.DTOs;
using TodoTaskAPI.Core.Entities;
using TodoTaskAPI.Infrastructure.Data;
using TodoTaskAPI.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace TodoTaskAPI.IntegrationTests.Endpoints
{
    /// <summary>
    /// Integration tests specifically focused on the Update Todo endpoint
    /// Tests the complete flow from HTTP request through all layers to database and back
    /// </summary>
    public class UpdateTodoIntegrationTests : IClassFixture<PostgreSqlContainerTest>
    {
        private readonly PostgreSqlContainerTest _fixture;
        private readonly HttpClient _client;
        private readonly ITestOutputHelper _output;

        public UpdateTodoIntegrationTests(PostgreSqlContainerTest fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _client = fixture.Factory.CreateClient();
            _output = output;
        }

        [Fact]
        public async Task UpdateTodo_WithAllFields_UpdatesSuccessfully()
        {
            var todo = await CreateTestTodo("Original Title");
            var updateDto = new UpdateTodoDto
            {
                Title = "Updated Title",
                Description = "Updated Description",
                ExpiryDateTime = DateTime.UtcNow.AddDays(5)
            };

            var response = await _client.PutAsJsonAsync($"/api/todos/{todo.Id}", updateDto);
            var content = await response.Content.ReadFromJsonAsync<ApiResponseDto<TodoDto>>();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(content?.Data);
            Assert.Equal(updateDto.Title, content.Data.Title);
            Assert.Equal(updateDto.Description, content.Data.Description);
            Assert.Equal(updateDto.ExpiryDateTime.Value.ToString("O"),
                content.Data.ExpiryDateTime.ToString("O"));

            using var scope = _fixture.Factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var updatedTodo = await dbContext.Todos.FindAsync(todo.Id);
            Assert.NotNull(updatedTodo);
            Assert.Equal(updateDto.Title, updatedTodo.Title);
        }

        [Fact]
        public async Task UpdateTodo_WithPartialFields_UpdatesOnlyProvidedFields()
        {
            var todo = await CreateTestTodo("Original Title");
            var originalDescription = todo.Description;
            var originalExpiryDateTime = todo.ExpiryDateTime;

            var updateDto = new UpdateTodoDto
            {
                Title = "Updated Title"
            };

            var response = await _client.PutAsJsonAsync($"/api/todos/{todo.Id}", updateDto);
            var content = await response.Content.ReadFromJsonAsync<ApiResponseDto<TodoDto>>();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(content?.Data);
            Assert.Equal(updateDto.Title, content.Data.Title);
            Assert.Equal(originalDescription, content.Data.Description);
            Assert.Equal(
                originalExpiryDateTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                content.Data.ExpiryDateTime.ToString("yyyy-MM-ddTHH:mm:ss")
            );
        }

        //[Theory]
        //[InlineData("", "Title cannot be empty")]
        //[InlineData("A very long title that exceeds the maximum lengthddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddd", "Title must be between 1 and 200 characters")]
        //public async Task UpdateTodo_WithInvalidData_ReturnsBadRequest(string invalidTitle, string expectedError)
        //{
        //    var todo = await CreateTestTodo("Original Title");
        //    var updateDto = new UpdateTodoDto { Title = invalidTitle };

        //    var response = await _client.PutAsJsonAsync($"/api/todos/{todo.Id}", updateDto);
        //    _output.WriteLine($"Response Status: {response.StatusCode}");
        //    var responseContent = await response.Content.ReadAsStringAsync();
        //    _output.WriteLine($"Response Content: {responseContent}");

        //    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        //    var content = JsonSerializer.Deserialize<ApiResponseDto<ValidationErrorResponse>>(responseContent);
        //    Assert.NotNull(content?.Data?.Errors);
        //    Assert.Contains(expectedError,
        //        content.Data.Errors.SelectMany(e => e.Value).First());
        //}

        [Fact]
        public async Task UpdateTodo_WithNonExistentId_ReturnsNotFound()
        {
            var updateDto = new UpdateTodoDto { Title = "Updated Title" };
            var response = await _client.PutAsJsonAsync($"/api/todos/{Guid.NewGuid()}", updateDto);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

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
                CreatedAt = DateTime.UtcNow,
                IsDone = false
            };

            context.Todos.Add(todo);
            await context.SaveChangesAsync();
            return todo;
        }
    }
}