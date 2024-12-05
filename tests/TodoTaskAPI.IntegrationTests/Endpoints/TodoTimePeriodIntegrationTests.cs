using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using TodoTaskAPI.Application.DTOs;
using TodoTaskAPI.Infrastructure.Data;
using TodoTaskAPI.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace TodoTaskAPI.IntegrationTests.Endpoints
{
    /// <summary>
    /// Integration tests for the upcoming todos endpoint
    /// Tests the complete flow from HTTP request to database and back
    /// </summary>
    public class TodoTimePeriodIntegrationTests : IClassFixture<PostgreSqlContainerTest>
    {
        private readonly PostgreSqlContainerTest _fixture;
        private readonly HttpClient _client;
        private readonly ITestOutputHelper _output;

        public TodoTimePeriodIntegrationTests(PostgreSqlContainerTest fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _client = fixture.Factory.CreateClient();
            _output = output;
        }

        [Fact]
        public async Task GetUpcoming_Today_ReturnsOnlyTodaysTodos()
        {
            // Arrange
            using var scope = _fixture.Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await SeedTestData(context);

            // Act
            var response = await _client.GetAsync("/api/todos/upcoming?period=Today");
            var content = await response.Content.ReadFromJsonAsync<ApiResponseDto<IEnumerable<TodoDto>>>();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(content?.Data);
            Assert.All(content.Data, todo =>
                Assert.Equal(DateTime.UtcNow.Date, todo.ExpiryDateTime.Date));
        }

        [Fact]
        public async Task GetUpcoming_CustomRange_ReturnsCorrectTodos()
        {
            // Arrange
            using var scope = _fixture.Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await SeedTestData(context);

            var startDate = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            var endDate = DateTime.UtcNow.AddDays(7).ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            var url = $"/api/todos/upcoming?period=Custom&startDate={startDate}&endDate={endDate}";

            _output.WriteLine($"Request URL: {url}"); // Dodajemy logging dla debugowania

            // Act
            var response = await _client.GetAsync(url);
            var responseContent = await response.Content.ReadAsStringAsync(); // Dodajemy logging odpowiedzi
            _output.WriteLine($"Response Status: {response.StatusCode}");
            _output.WriteLine($"Response Content: {responseContent}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadFromJsonAsync<ApiResponseDto<IEnumerable<TodoDto>>>();
            Assert.NotNull(content?.Data);
            Assert.All(content.Data!, todo =>
            {
                var todoDate = todo.ExpiryDateTime.ToUniversalTime();
                var start = DateTime.Parse(startDate).ToUniversalTime();
                var end = DateTime.Parse(endDate).ToUniversalTime();

                Assert.True(todoDate.Date >= start.Date);
                Assert.True(todoDate.Date <= end.Date);
            });
        }

        [Fact]
        public async Task GetUpcoming_InvalidCustomRange_ReturnsBadRequest()
        {
            // Arrange
            var startDate = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd"); // Past date
            var endDate = DateTime.UtcNow.AddDays(7).ToString("yyyy-MM-dd");
            var url = $"/api/todos/upcoming?period=Custom&startDate={startDate}&endDate={endDate}";

            // Act
            var response = await _client.GetAsync(url);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var content = await response.Content.ReadFromJsonAsync<ApiResponseDto<ValidationErrorResponse>>();
            Assert.NotNull(content?.Data?.Errors);
            Assert.Contains("past", content.Message.ToLower());
        }

        [Fact]
        public async Task GetUpcoming_CurrentWeek_HandlesConcurrentRequests()
        {
            // Arrange
            using var scope = _fixture.Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await SeedTestData(context);

            // Act - Send multiple concurrent requests
            var tasks = new List<Task<HttpResponseMessage>>();
            for (int i = 0; i < 5; i++)
            {
                tasks.Add(_client.GetAsync("/api/todos/upcoming?period=CurrentWeek"));
            }

            var responses = await Task.WhenAll(tasks);

            // Assert
            Assert.All(responses, response =>
            {
                Assert.True(
                    response.StatusCode == HttpStatusCode.OK ||
                    response.StatusCode == HttpStatusCode.TooManyRequests);
            });
        }

        [Theory]
        [InlineData("Today")]
        [InlineData("Tomorrow")]
        [InlineData("CurrentWeek")]
        public async Task GetUpcoming_DifferentPeriods_ReturnsSuccessfully(string period)
        {
            // Arrange
            using var scope = _fixture.Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await SeedTestData(context);

            // Act
            var response = await _client.GetAsync($"/api/todos/upcoming?period={period}");
            var content = await response.Content.ReadFromJsonAsync<ApiResponseDto<IEnumerable<TodoDto>>>();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(content?.Data);
        }

        [Fact]
        public async Task GetUpcoming_WithLargeDataset_MaintainsPerformance()
        {
            // Arrange
            using var scope = _fixture.Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await SeedLargeTestDataset(context);

            var startDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            var endDate = DateTime.UtcNow.AddMonths(1).ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            var url = $"/api/todos/upcoming?period=Custom&startDate={startDate}&endDate={endDate}";

            _output.WriteLine($"Request URL: {url}"); // Dodajemy logging dla debugowania

            // Act
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var response = await _client.GetAsync(url);
            var responseContent = await response.Content.ReadAsStringAsync();
            sw.Stop();

            _output.WriteLine($"Response Status: {response.StatusCode}");
            _output.WriteLine($"Response Content: {responseContent}");
            _output.WriteLine($"Request took: {sw.ElapsedMilliseconds}ms");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(sw.ElapsedMilliseconds < 1000,
                $"Request took {sw.ElapsedMilliseconds}ms");
        }

        private async Task SeedTestData(ApplicationDbContext context)
        {
            var todos = new[]
            {
            new TodoTaskAPI.Core.Entities.Todo
            {
                Id = Guid.NewGuid(),
                Title = "Today's Todo",
                Description = "Test Description",
                ExpiryDateTime = DateTime.UtcNow,
                PercentComplete = 0,
                CreatedAt = DateTime.UtcNow
            },
            new TodoTaskAPI.Core.Entities.Todo
            {
                Id = Guid.NewGuid(),
                Title = "Tomorrow's Todo",
                Description = "Test Description",
                ExpiryDateTime = DateTime.UtcNow.AddDays(1),
                PercentComplete = 0,
                CreatedAt = DateTime.UtcNow
            },
            new TodoTaskAPI.Core.Entities.Todo
            {
                Id = Guid.NewGuid(),
                Title = "Next Week's Todo",
                Description = "Test Description",
                ExpiryDateTime = DateTime.UtcNow.AddDays(7),
                PercentComplete = 0,
                CreatedAt = DateTime.UtcNow
            }
        };

            await context.Database.EnsureDeletedAsync(); // Dodane czyszczenie bazy
            await context.Database.EnsureCreatedAsync();
            await context.Todos.AddRangeAsync(todos);
            await context.SaveChangesAsync();
        }

        private async Task SeedLargeTestDataset(ApplicationDbContext context)
        {
            var todos = new List<TodoTaskAPI.Core.Entities.Todo>();
            var baseDate = DateTime.UtcNow;

            for (int i = 0; i < 100; i++)
            {
                todos.Add(new TodoTaskAPI.Core.Entities.Todo
                {
                    Id = Guid.NewGuid(),
                    Title = $"Performance Test Todo {i}",
                    Description = $"Description for todo {i}",
                    ExpiryDateTime = baseDate.AddDays(i % 60),
                    PercentComplete = i % 100,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await context.Database.EnsureDeletedAsync(); // Dodane czyszczenie bazy
            await context.Database.EnsureCreatedAsync();
            await context.Todos.AddRangeAsync(todos);
            await context.SaveChangesAsync();
        }
    }
}
