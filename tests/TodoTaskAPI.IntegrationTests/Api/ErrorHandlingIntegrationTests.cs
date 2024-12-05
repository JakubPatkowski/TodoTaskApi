using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using TodoTaskAPI.Application.DTOs;
using Xunit.Abstractions;
using Xunit;
using System.Text.Json;
using TodoTaskAPI.IntegrationTests.Infrastructure;

namespace TodoTaskAPI.IntegrationTests.Api
{
    /// <summary>
    /// Integration tests focusing on error handling middleware with real database and HTTP pipeline
    /// </summary>
    public class ErrorHandlingIntegrationTests : IClassFixture<PostgreSqlContainerTest>
    {
        private readonly HttpClient _client;
        private readonly ITestOutputHelper _output;

        public ErrorHandlingIntegrationTests(PostgreSqlContainerTest fixture, ITestOutputHelper output)
        {
            _client = fixture.Factory.CreateClient();
            _output = output;
        }

        [Fact]
        public async Task EndToEnd_ValidationError_ReturnsProperErrorResponse()
        {
            var invalidTodo = new CreateTodoDto
            {
                Title = "Test Todo",
                ExpiryDateTime = DateTime.UtcNow.AddDays(-1) // Przeszła data
            };

            var response = await _client.PostAsJsonAsync("/api/todos", invalidTodo);
            _output.WriteLine($"Response status code: {response.StatusCode}");

            var content = await response.Content.ReadAsStringAsync();
            _output.WriteLine($"Response content: {content}");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            // Deserializuj do dynamic aby obsłużyć różne formaty odpowiedzi
            var errorContent = JsonDocument.Parse(content);
            Assert.NotNull(errorContent);

            // Sprawdź czy zawiera informację o błędzie walidacji
            Assert.Contains("ExpiryDateTime", content);
            Assert.Contains("future", content);
        }
    }
}

