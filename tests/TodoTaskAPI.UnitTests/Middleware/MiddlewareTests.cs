using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TodoTaskAPI.API.Middleware;
using TodoTaskAPI.Core.Exceptions;

namespace TodoTaskAPI.UnitTests.Middleware
{
    /// <summary>
    /// Integration tests for API middleware components
    /// </summary>
    public class MiddlewareTests
    {
        /// <summary>
        /// Verifies that ErrorHandlingMiddleware catches exceptions and returns properly formatted error responses
        /// </summary>
        [Fact]
        public async Task ErrorHandlingMiddleware_CatchesAndFormatsException()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            var middleware = new ErrorHandlingMiddleware(
                next: (innerHttpContext) => Task.FromException(new Exception("Test error")),
                new Mock<ILogger<ErrorHandlingMiddleware>>().Object
            );

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var reader = new StreamReader(context.Response.Body);
            var responseBody = await reader.ReadToEndAsync();
            var response = JsonSerializer.Deserialize<Dictionary<string, object>>(responseBody);

            Assert.Equal(500, context.Response.StatusCode);
            Assert.Contains("Test error", responseBody);
        }

        /// <summary>
        /// Verifies that RateLimitingMiddleware correctly enforces rate limits under concurrent load
        /// by checking if at least one request received 429 status code
        /// </summary>
        [Fact]
        public async Task RateLimitingMiddleware_EnforcesRateLimit()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<RateLimitingMiddleware>>();
            var middleware = new RateLimitingMiddleware(
                next: _ => Task.CompletedTask,
                mockLogger.Object
            );
            var rateLimitHit = false;

            // Act
            var tasks = new List<Task>();
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var context = new DefaultHttpContext();
                    context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");

                    for (int j = 0; j < 15; j++) // Zwiększona liczba zapytań
                    {
                        await middleware.InvokeAsync(context);
                        if (context.Response.StatusCode == StatusCodes.Status429TooManyRequests)
                        {
                            rateLimitHit = true;
                        }
                    }
                }));
            }
            await Task.WhenAll(tasks);

            // Assert
            Assert.True(rateLimitHit, "Rate limit was never hit during the test");
        }


        /// <summary>
        /// Verifies that RequestLoggingMiddleware properly logs HTTP request details
        /// </summary>
        [Fact]
        public async Task RequestLoggingMiddleware_LogsRequestDetails()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Method = "GET";
            context.Request.Path = "/api/todos";

            var mockLogger = new Mock<ILogger<RequestLoggingMiddleware>>();
            var middleware = new RequestLoggingMiddleware(
                next: (innerHttpContext) => Task.CompletedTask,
                mockLogger.Object
            );

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Request GET /api/todos")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
                Times.AtLeast(1)
            );
        }

        /// <summary>
        /// Verifies that ErrorHandlingMiddleware handles ValidationException correctly
        /// </summary>
        [Fact]
        public async Task ErrorHandlingMiddleware_HandlesValidationException()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            var mockLogger = new Mock<ILogger<ErrorHandlingMiddleware>>();

            var middleware = new ErrorHandlingMiddleware(
                next: _ => Task.FromException(new TodoTaskAPI.Core.Exceptions.ValidationException("Validation error")),
                mockLogger.Object
            );

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var reader = new StreamReader(context.Response.Body);
            var responseBody = await reader.ReadToEndAsync();

            Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
            Assert.Contains("Validation error", responseBody);
        }

        /// <summary>
        /// Verifies that RateLimitingMiddleware correctly implements token bucket refill
        /// </summary>
        [Fact]
        public async Task RateLimitingMiddleware_TokenBucketRefills()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<RateLimitingMiddleware>>();
            var middleware = new RateLimitingMiddleware(
                next: _ => Task.CompletedTask,
                mockLogger.Object
            );

            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");

            // Act - First wave of requests
            for (int i = 0; i < 5; i++)
            {
                await middleware.InvokeAsync(context);
            }

            // Wait for token refill
            await Task.Delay(1100); // Wait slightly more than 1 second for token refill

            // Act - Second wave of requests after refill
            await middleware.InvokeAsync(context);

            // Assert
            Assert.NotEqual(StatusCodes.Status429TooManyRequests, context.Response.StatusCode);
        }

        /// <summary>
        /// Verifies that RateLimitingMiddleware sets correct response headers
        /// </summary>
        [Fact]
        public async Task RateLimitingMiddleware_SetsCorrectHeaders()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<RateLimitingMiddleware>>();
            var middleware = new RateLimitingMiddleware(
                next: _ => Task.CompletedTask,
                mockLogger.Object
            );

            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.True(context.Response.Headers.ContainsKey("X-RateLimit-Remaining"));
            if (context.Response.StatusCode == StatusCodes.Status429TooManyRequests)
            {
                Assert.True(context.Response.Headers.ContainsKey("Retry-After"));
                Assert.True(context.Response.Headers.ContainsKey("X-RateLimit-Reset"));
            }
        }

    }
}
