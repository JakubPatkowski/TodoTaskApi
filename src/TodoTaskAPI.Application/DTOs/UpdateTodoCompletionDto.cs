using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TodoTaskAPI.Application.DTOs
{
    /// <summary>
    /// Data Transfer Object for updating todo completion status
    /// Validates percentage is within valid range and handles completion state
    /// </summary>
    public class UpdateTodoCompletionDto
    {
        /// <summary>
        /// Completion percentage must be between 0 and 100
        /// </summary>
        [Range(0, 100, ErrorMessage = "Percentage must be between 0 and 100")]
        public int PercentComplete { get; set; }
    }
}
