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

namespace TodoTaskAPI.IntegrationTests.Endpoints
{
    public class DeleteTodoIntegrationTests : IClassFixture<PostgreSqlContainerTest>
    {
        private readonly PostgreSqlContainerTest _fixture;
        private readonly HttpClient _client;
        private readonly ITestOutputHelper _output;

        public DeleteTodoIntegrationTests(PostgreSqlContainerTest fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _client = fixture.Factory.CreateClient();
            _output = output;
        }

        [Fact]
        public async Task DeleteTodo_WithExistingId_ReturnsSuccess()
        {
            // Arrange
            var todo = await CreateTestTodo("Todo to Delete");

            // Act
            var response = await _client.DeleteAsync($"/api/todos/{todo.Id}");
            var content = await response.Content.ReadFromJsonAsync<ApiResponseDto<object>>();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(content);
            Assert.Equal($"Todo o ID {todo.Id} został pomyślnie usunięty.", content.Message);

            // Sprawdź, czy todo nie istnieje już w bazie danych
            using var scope = _fixture.Factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var deletedTodo = await dbContext.Todos.FindAsync(todo.Id);
            Assert.Null(deletedTodo);
        }

        [Fact]
        public async Task DeleteTodo_WithNonExistentId_ReturnsNotFound()
        {
            // Act
            var response = await _client.DeleteAsync($"/api/todos/{Guid.NewGuid()}");
            var content = await response.Content.ReadFromJsonAsync<ApiResponseDto<object>>();

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.NotNull(content);
            Assert.Contains("nie został znaleziony", content.Message);
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

            _output.WriteLine($"Utworzono todo z ID: {todo.Id} i tytułem: {todo.Title}");
            return todo;
        }
    }
}
