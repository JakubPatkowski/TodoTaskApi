using Microsoft.AspNetCore.Http;
using System;
using System.Diagnostics;
using TodoTaskAPI.API.Helpers;

namespace TodoTaskAPI.API.Middleware
{
    /// <summary>
    /// Middleware for logging details of each HTTP request and its response.
    /// Provides insights into request duration, request data, and response status.
    /// </summary>
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestLoggingMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next middleware in the HTTP request pipeline.</param>
        /// <param name="logger">Logger instance for capturing request details.</param>
        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        /// <summary>
        /// Middleware entry point, called for each HTTP request.
        /// Logs request details and measures the time taken to process the request.
        /// </summary>
        /// <param name="context">The current HTTP context.</param>
        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var requestBody = "";

                // Only read the body for non-GET requests to avoid performance impact
                if(!context.Request.Method.Equals("GET", StringComparison.OrdinalIgnoreCase))
                {
                    context.Request.EnableBuffering();
                    using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
                    requestBody = await reader.ReadToEndAsync();
                    context.Request.Body.Position = 0;
                }

                // Log request details
                _logger.LogInformation(
                    "Request {Method} {Path} started at {Time}. Query: {Query}. Body: {Body}",
                    LogSanitizer.Sanitize(context.Request.Method),
                    LogSanitizer.Sanitize(context.Request.Path),
                    DateTime.UtcNow,
                    LogSanitizer.Sanitize(context.Request.QueryString),
                    LogSanitizer.Sanitize(requestBody));

                // Pass control to the next middleware in the pipeline.
                await _next(context);

                stopwatch.Stop();

                // Log the response details, including the elapsed time.
                _logger.LogInformation(
                     "Request {Method} {Path} completed in {ElapsedMilliseconds}ms with status code {StatusCode}",
                     context.Request.Method,
                     context.Request.Path,
                     stopwatch.ElapsedMilliseconds,
                     context.Response.StatusCode);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                // Log the exception and the time it took before failure.
                _logger.LogError(
                    ex,
                    "Request {Method} {Path} failed after {ElapsedMilliseconds}ms",
                    LogSanitizer.Sanitize(context.Request.Method),
                    LogSanitizer.Sanitize(context.Request.Path),
                    stopwatch.ElapsedMilliseconds);
                throw;   // Re-throw the exception to propagate it up the middleware pipeline.
            }
        }
    }
}
