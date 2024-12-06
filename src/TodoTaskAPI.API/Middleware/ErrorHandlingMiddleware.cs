using System.ComponentModel.DataAnnotations;
using TodoTaskAPI.Application.DTOs;

namespace TodoTaskAPI.API.Middleware
{
    /// <summary>
    /// Middleware responsible for handling exceptions globally within the application pipeline.
    /// Ensures that all exceptions are logged and proper HTTP responses are sent back to the client.
    /// </summary>
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorHandlingMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next middleware in the HTTP request pipeline.</param>
        /// <param name="logger">Logger to capture exception details.</param>
        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        /// <summary>
        /// Middleware entry point, called for each HTTP request.
        /// Wraps the execution of the pipeline with a try-catch block to handle exceptions.
        /// </summary>
        /// <param name="context">The current HTTP context.</param>
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // Pass control to the next middleware in the pipeline.
                await _next(context);
            }
            catch (TodoTaskAPI.Core.Exceptions.ValidationException ex)
            {
                // Log the validation exception at the warning level.
                _logger.LogWarning(ex, "Validation error occurred");

                // Set the HTTP response status code to 400 (Bad Request) for validation errors.
                context.Response.StatusCode = StatusCodes.Status400BadRequest;

                // Serialize and return a standardized error response to the client.
                await HandleExceptionAsync(context, ex);
            }
            catch (Exception ex)
            {
                // Log unexpected exceptions at the error level.
                _logger.LogError(ex, "An unexpected error occurred");

                // Set the HTTP response status code to 500 (Internal Server Error) for generic errors.
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;

                // Serialize and return a standardized error response to the client.
                await HandleExceptionAsync(context, ex);
            }
        }



        /// <summary>
        /// Handles the serialization of exceptions into a standardized JSON response.
        /// </summary>
        /// <param name="context">The current HTTP context.</param>
        /// <param name="exception">The exception to handle.</param>
        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            // Set the content type to JSON for the response.
            context.Response.ContentType = "application/json";

            // Generate a response object based on the type of exception.
            object response = exception switch
            {
                // Handle custom validation exceptions.
                ValidationException validationEx => new ApiResponseDto<ValidationErrorResponse>
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = validationEx.Message,
                    Data = new ValidationErrorResponse
                    {
                        Errors = new Dictionary<string, string[]>
                {
                    { "Validation", new[] { validationEx.Message } }
                }
                    }
                },

                // Handle other generic exceptions.
                _ => new ApiResponseDto<object>
                {
                    StatusCode = context.Response.StatusCode,
                    Message = exception.Message
                }
            };

            // Write the response object as JSON to the HTTP response body.
            await context.Response.WriteAsJsonAsync(response);
        }
    }
}
