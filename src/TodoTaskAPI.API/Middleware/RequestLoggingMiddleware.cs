using System.Diagnostics;

namespace TodoTaskAPI.API.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

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
                    context.Request.Method,
                    context.Request.Path,
                    DateTime.UtcNow,
                    context.Request.QueryString,
                    requestBody);

                await _next(context);

                stopwatch.Stop();

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
                _logger.LogError(
                    ex,
                    "Request {Method} {Path} failed after {ElapsedMilliseconds}ms",
                    context.Request.Method,
                    context.Request.Path,
                    stopwatch.ElapsedMilliseconds);
                throw;
            }
        }
    }
}
