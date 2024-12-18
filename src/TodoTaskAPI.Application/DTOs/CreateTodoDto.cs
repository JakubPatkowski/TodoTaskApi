﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using TodoTaskAPI.Application.Constants;

namespace TodoTaskAPI.Application.DTOs
{
    /// <summary>
    /// Data Transfer Object for creating a new Todo item
    /// </summary>
    public class CreateTodoDto
    {
        /// <summary>
        /// Date and time when the todo should be completed
        /// Must be in the future
        /// </summary>
        /// <example>2024-12-31T23:59:59Z</example>
        [Required(ErrorMessage = "Expiry date and time is required")]
        [CustomValidation(typeof(CreateTodoDto), nameof(ValidateExpiryDateTime))]
        public DateTime ExpiryDateTime { get; set; }

        /// <summary>
        /// Title of the todo item
        /// </summary> 
        /// <example>Title</example>
        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, MinimumLength = 1, ErrorMessage = "Title must be between 1 and 200 characters")]
        [RegularExpression(ValidationConstants.TitleRegexPattern,
        ErrorMessage = ValidationConstants.TitleRegexErrorMessage)]
        public required string Title { get; set; }

        /// <summary>
        /// Detailed description of the todo item (optional)
        /// </summary>
        /// <example>Description</example>
        [StringLength(1000, MinimumLength = 0, ErrorMessage = "Description must be between 0 and 1000 characters")]
        public string? Description { get; set; } = string.Empty;

        /// <summary>
        /// Completion percentage of the todo item (optional)
        /// </summary>
        /// <example>0</example>
        [Range(0, 100, ErrorMessage = "Percent complete must be between 0 and 100")]
        public int? PercentComplete { get; set; }

        /// <summary>
        /// Validates that ExpiryDateTime is in the future
        /// </summary>
        public static ValidationResult? ValidateExpiryDateTime(DateTime expiryDateTime, ValidationContext context)
        {
            if (expiryDateTime <= DateTime.UtcNow)
            {
                return new ValidationResult("Expiry date must be in the future");
            }
            return ValidationResult.Success;
        }

    }
}
