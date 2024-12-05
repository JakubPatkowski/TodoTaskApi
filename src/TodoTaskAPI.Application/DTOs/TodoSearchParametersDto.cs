﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TodoTaskAPI.Application.Constants;

namespace TodoTaskAPI.Application.DTOs
{
    /// <summary>
    /// Data Transfer Object for searching Todo items
    /// </summary>
    public class TodoSearchParametersDto
    {
        /// <summary>
        /// Optional Todo ID to search by
        /// </summary>
        public Guid? Id { get; set; }

        /// <summary>
        /// Optional Todo title to search by (case-insensitive, exact match)
        /// </summary>
        [StringLength(200, ErrorMessage = "Title search parameter cannot exceed 200 characters")]
        [RegularExpression(ValidationConstants.TitleRegexPattern,
            ErrorMessage = ValidationConstants.TitleRegexErrorMessage)]
        public string? Title { get; set; }

        /// <summary>
        /// Validates that at least one search parameter is provided and title format is valid
        /// </summary>
        public void ValidateParameters()
        {
            if (!Id.HasValue && string.IsNullOrWhiteSpace(Title))
            {
                throw new TodoTaskAPI.Core.Exceptions.ValidationException(
                    "At least one search parameter (Id or Title) must be provided");
            }

            if (!string.IsNullOrWhiteSpace(Title))
            {
                // Dodatkowa walidacja tytułu
                if (Title.Any(c => char.IsControl(c) || c == '/' || c == '\\'))
                {
                    throw new TodoTaskAPI.Core.Exceptions.ValidationException(
                        "Title cannot contain control characters or path separators");
                }

                // Sprawdzanie potencjalnych XSS i SQL injection
                var dangerousPatterns = new[] { "<", ">", "script", "=", "--", "'" };
                if (dangerousPatterns.Any(pattern => Title.Contains(pattern, StringComparison.OrdinalIgnoreCase)))
                {
                    throw new TodoTaskAPI.Core.Exceptions.ValidationException(
                        "Title contains invalid characters or potentially dangerous patterns");
                }

                // Weryfikacja wzorca regex
                if (!System.Text.RegularExpressions.Regex.IsMatch(Title, ValidationConstants.TitleRegexPattern))
                {
                    throw new TodoTaskAPI.Core.Exceptions.ValidationException(
                        ValidationConstants.TitleRegexErrorMessage);
                }
            }
        }
    }
}