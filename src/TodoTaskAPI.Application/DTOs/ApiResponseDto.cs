using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;


namespace TodoTaskAPI.Application.DTOs
{
    /// <summary>
    /// Standardized API response wrapper
    /// </summary>
    /// <typeparam name="T">Type of data being returned</typeparam>
    public class ApiResponseDto<T>
    {
        /// <summary>
        /// Response status code
        /// </summary>

        public static class StatusCodes
        {
            public const int Success = 200;
            public const int BadRequest = 400;
            public const int NotFound = 404;
            public const int InternalServerError = 500;
        }
        public int StatusCode { get; set; }

        /// <summary>
        /// Response message
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// Optional response data
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public T? Data { get; set; }

        /// <summary>
        /// Creates a successful response with data
        /// </summary>
        /// <param name="data">Data to be included in the response</param>
        /// <param name="message">Optional success message</param>
        public static ApiResponseDto<T> Success(T data, string message = "Success")
        {
            return new ApiResponseDto<T>
            {
                StatusCode = StatusCodes.Success,
                Message = message,
                Data = data
            };
        }

        /// <summary>
        /// Creates a failed response
        /// </summary>
        public static ApiResponseDto<T> Failure(int statusCode, string message)
        {
            return new ApiResponseDto<T>
            {
                StatusCode = statusCode,
                Message = message,
                Data = default
            };
        }
    }
}
