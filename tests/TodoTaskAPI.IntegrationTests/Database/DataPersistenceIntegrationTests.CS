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

namespace TodoTaskAPI.IntegrationTests.Database
{
    /// <summary>
    /// Integration tests verifying data persistence and retrieval across application restarts
    /// These tests ensure that data is properly saved and can be retrieved even after
    /// the application context is renewed
    /// </summary>
    public class DataPersistenceIntegrationTests : IClassFixture<PostgreSqlContainerTest>
    {
        private readonly PostgreSqlContainerTest _fixture;
        private readonly ITestOutputHelper _output;

        public DataPersistenceIntegrationTests(PostgreSqlContainerTest fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _output = output;
        }

        [Fact]
        public async Task EndToEnd_DataPersistence_SurvivesApplicationRestart()
        {
            // Arrange - Create first client
            var firstClient = _fixture.Factory.CreateClient();
            var todo = new CreateTodoDto
            {
                Title = "Persistence Test Todo",
                Description = "Testing data survival",
                ExpiryDateTime = DateTime.UtcNow.AddDays(1),
                PercentComplete = 0
            };

            // Act - Create todo with first client
            var createResponse = await firstClient.PostAsJsonAsync("/api/todos", todo);
            var createContent = await createResponse.Content.ReadFromJsonAsync<ApiResponseDto<TodoDto>>();
            var todoId = createContent?.Data?.Id;

            _output.WriteLine($"Created todo with ID: {todoId}");

            // Create new client (simulating application restart)
            var secondClient = _fixture.Factory.CreateClient();

            // Verify data persists
            var getResponse = await secondClient.GetAsync("/api/todos");
            var getAllContent = await getResponse.Content.ReadFromJsonAsync<ApiResponseDto<IEnumerable<TodoDto>>>();

            // Assert
            var persistedTodo = getAllContent?.Data?.FirstOrDefault(t => t.Id == todoId);
            Assert.NotNull(persistedTodo);
            Assert.Equal(todo.Title, persistedTodo.Title);
        }
    }
}
