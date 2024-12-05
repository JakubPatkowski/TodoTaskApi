using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Json;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using TodoTaskAPI.Application.DTOs;
using Xunit.Abstractions;
using Xunit;
using TodoTaskAPI.IntegrationTests.Infrastructure;

namespace TodoTaskAPI.IntegrationTests.Performance
{
    /// <summary>
    /// Integration tests focusing on system performance under various conditions
    /// Verifies response times and system behavior under load
    /// </summary>
    public class PerformanceIntegrationTests : IClassFixture<PostgreSqlContainerTest>
    {
        private readonly HttpClient _client;
        private readonly ITestOutputHelper _output;

        public PerformanceIntegrationTests(PostgreSqlContainerTest fixture, ITestOutputHelper output)
        {
            _client = fixture.Factory.CreateClient();
            _output = output;
        }

        /// <summary>
        /// Verifies system performance with large result sets
        /// Tests pagination and response times with significant data volume
        /// </summary>
        [Fact]
        public async Task EndToEnd_LargeResultSet_MaintainsPerformance()
        {
            // Arrange - Create 100 todos
            var todos = Enumerable.Range(1, 100).Select(i => new CreateTodoDto
            {
                Title = $"Performance Test Todo {i}",
                ExpiryDateTime = DateTime.UtcNow.AddDays(i),
                PercentComplete = 0
            });

            foreach (var todo in todos)
            {
                await _client.PostAsJsonAsync("/api/todos", todo);
            }

            // Act
            var sw = Stopwatch.StartNew();
            var response = await _client.GetAsync("/api/todos?pageNumber=1&pageSize=50");
            sw.Stop();

            // Assert
            Assert.True(sw.ElapsedMilliseconds < 1000, "Response time should be under 1 second");
            Assert.True(response.IsSuccessStatusCode);
        }

        /// <summary>
        /// Tests system behavior under sustained load with mixed operations
        /// Verifies system stability with concurrent reads and writes
        /// </summary>
        [Fact]
        public async Task EndToEnd_SustainedLoad_HandlesCorrectly()
        {
            // Arrange
            var operations = new List<Func<Task<HttpResponseMessage>>>();

            // Mix of read and write operations
            for (int i = 0; i < 20; i++)
            {
                if (i % 2 == 0)
                {
                    operations.Add(() => _client.GetAsync("/api/todos"));
                }
                else
                {
                    var todo = new CreateTodoDto
                    {
                        Title = $"Load Test Todo {i}",
                        ExpiryDateTime = DateTime.UtcNow.AddDays(1),
                        PercentComplete = 0
                    };
                    operations.Add(() => _client.PostAsJsonAsync("/api/todos", todo));
                }
            }

            // Act
            var sw = Stopwatch.StartNew();
            var tasks = operations.Select(operation => operation());
            var responses = await Task.WhenAll(tasks);
            sw.Stop();

            // Log performance metrics
            _output.WriteLine($"Total time for {operations.Count} operations: {sw.ElapsedMilliseconds}ms");
            _output.WriteLine($"Average time per operation: {sw.ElapsedMilliseconds / operations.Count}ms");

            // Assert
            Assert.True(sw.ElapsedMilliseconds < 5000, "Batch operations should complete within 5 seconds");
            Assert.All(responses, r => Assert.True(r.IsSuccessStatusCode || r.StatusCode == HttpStatusCode.TooManyRequests));
        }
    }
}
