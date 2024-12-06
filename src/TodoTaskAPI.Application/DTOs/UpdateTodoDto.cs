using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TodoTaskAPI.Application.Constants;

namespace TodoTaskAPI.Application.DTOs
{
    /// <summary>
    /// Data Transfer Object for updating a Todo item
    /// Allows partial updates where only provided properties will be modified
    /// </summary>
    public class UpdateTodoDto
    {
        /// <summary>
        /// Optional new title for the todo
        /// </summary>
        [Required(ErrorMessage = "Title cannot be empty")]
        [StringLength(200, MinimumLength = 1, ErrorMessage = "Title must be between 1 and 200 characters")]
        [RegularExpression(ValidationConstants.TitleRegexPattern,
        ErrorMessage = ValidationConstants.TitleRegexErrorMessage)]
        public string? Title { get; set; }

        /// <summary>
        /// Optional new description for the todo
        /// </summary>
        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string? Description { get; set; }

        /// <summary>
        /// Optional new expiry date and time
        /// </summary>
        [CustomValidation(typeof(UpdateTodoDto), nameof(ValidateExpiryDateTime))]
        public DateTime? ExpiryDateTime { get; set; }

        /// <summary>
        /// Optional new completion percentage
        /// </summary>
        [Range(0, 100, ErrorMessage = "Percent complete must be between 0 and 100")]
        public int? PercentComplete { get; set; }

        /// <summary>
        /// Optional flag to mark todo as done/undone
        /// </summary>
        public bool? IsDone { get; set; }

        /// <summary>
        /// Validates that if ExpiryDateTime is provided, it's in the future
        /// </summary>
        public static ValidationResult? ValidateExpiryDateTime(DateTime? expiryDateTime, ValidationContext context)
        {
            if (expiryDateTime.HasValue && expiryDateTime <= DateTime.UtcNow)
            {
                return new ValidationResult("Expiry date must be in the future");
            }
            return ValidationResult.Success;
        }

        /// <summary>
        /// Validates that at least one property is set for update
        /// </summary>
        public void ValidateAtLeastOnePropertySet()
        {
            if (Title == null &&
                Description == null &&
                ExpiryDateTime == null &&
                PercentComplete == null &&
                IsDone == null)
            {
                throw new TodoTaskAPI.Core.Exceptions.ValidationException(
                    "At least one property must be provided for update");
            }
        }
    }
}
