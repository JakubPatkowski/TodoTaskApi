using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TodoTaskAPI.Application.DTOs
{
    // TodoTaskAPI.Application.DTOs/ErrorResponseDto.cs
    /// <summary>
    /// Standardized error response model
    /// </summary>
    public class ErrorResponseDto
    {
        /// <summary>
        /// Error message describing what went wrong
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// HTTP status code
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// Optional technical details (only included in development)
        /// </summary>
        public string Details { get; set; }
    }
}
