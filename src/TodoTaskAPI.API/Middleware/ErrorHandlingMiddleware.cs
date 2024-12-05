using System.ComponentModel.DataAnnotations;
using TodoTaskAPI.Application.DTOs;

namespace TodoTaskAPI.API.Middleware
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;

        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (TodoTaskAPI.Core.Exceptions.ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error occurred");
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await HandleExceptionAsync(context, ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred");
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            object response = exception switch
            {
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
                _ => new ApiResponseDto<object>
                {
                    StatusCode = context.Response.StatusCode,
                    Message = exception.Message
                }
            };

            await context.Response.WriteAsJsonAsync(response);
        }
    }
}
