using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using TodoTaskAPI.IntegrationTests.Infrastructure;
using Xunit;

namespace TodoTaskAPI.IntegrationTests.Performance
{
    /// <summary>
    /// Integration tests for rate limiting functionality under real load conditions
    /// </summary>
    public class RateLimitingIntegrationTests : IClassFixture<PostgreSqlContainerTest>
    {
        private readonly HttpClient _client;

        public RateLimitingIntegrationTests(PostgreSqlContainerTest fixture)
        {
            _client = fixture.Factory.CreateClient();
        }

        [Fact]
        public async Task EndToEnd_RateLimiting_EnforcesLimitsAcrossEndpoints()
        {
            // Arrange
            var tasks = new List<Task<HttpResponseMessage>>();
            var endpoints = new[] { "/api/todos", "/api/todos?pageNumber=1&pageSize=10" };

            // Act - Send rapid requests to different endpoints
            foreach (var endpoint in endpoints)
            {
                for (int i = 0; i < 15; i++)
                {
                    tasks.Add(_client.GetAsync(endpoint));
                }
            }

            var responses = await Task.WhenAll(tasks);

            // Assert
            var rateLimitedResponses = responses.Count(r => r.StatusCode == HttpStatusCode.TooManyRequests);
            Assert.True(rateLimitedResponses > 0, "Rate limiting should trigger for rapid requests");

            // Verify rate limit headers
            var limitedResponse = responses.First(r => r.StatusCode == HttpStatusCode.TooManyRequests);
            Assert.True(limitedResponse.Headers.Contains("Retry-After"));
        }
    }
}
