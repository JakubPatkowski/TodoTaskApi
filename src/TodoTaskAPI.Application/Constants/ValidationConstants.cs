using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TodoTaskAPI.Application.Constants
{
    /// <summary>
    /// Shared validation constants for DTOs
    /// </summary>
    public static class ValidationConstants
    {
        /// <summary>
        /// Regex pattern for validating todo titles
        /// Allows letters (including Polish), numbers, spaces, and specific punctuation marks
        /// </summary>
        public const string TitleRegexPattern = @"^[a-zA-ZżźćńółęąśŻŹĆĄŚĘŁÓŃ0-9\s\-_.!?,:;()]{1,200}$";

        /// <summary>
        /// Error message for invalid title format
        /// </summary>
        public const string TitleRegexErrorMessage =
            "Title can only contain letters (including Polish), numbers, spaces, and basic punctuation (-_.!?,:;())";
    }
}
