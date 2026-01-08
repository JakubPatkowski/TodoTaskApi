using System.Text.RegularExpressions;

namespace TodoTaskAPI.Core.Helpers
{
    /// <summary>
    /// Utility class for sanitizing log inputs to prevent log injection attacks.
    /// Removes potentially dangerous characters and patterns from strings before logging.
    /// </summary>
    /// <remarks>
    /// Log injection attacks can manipulate log files by injecting newlines,
    /// control characters, or encoded characters. This class helps prevent such attacks.
    /// </remarks>
    public static class LogSanitizer
    {
        private const int MaxLength = 500;
        
        /// <summary>
        /// Sanitizes a string value for safe logging by removing dangerous characters.
        /// </summary>
        /// <param name="value">The string value to sanitize</param>
        /// <returns>A sanitized string safe for logging, or "[null]" if input is null</returns>
        /// <remarks>
        /// Removes: newlines, carriage returns, tabs, and URL-encoded variants.
        /// Truncates to 500 characters max.
        /// </remarks>
        public static string Sanitize(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return "[null]";

            // Remove newlines and carriage returns (prevents log line injection)
            var sanitized = value
                .Replace("\r", "")
                .Replace("\n", "")
                .Replace("\t", " ");

            // Remove URL-encoded newlines
            sanitized = Regex.Replace(sanitized, @"%0[aAdD]", "", RegexOptions.IgnoreCase);
            
            // Remove other potentially dangerous patterns
            sanitized = Regex.Replace(sanitized, @"[\x00-\x1F\x7F]", ""); // Control characters

            // Truncate to prevent log flooding
            if (sanitized.Length > MaxLength)
            {
                sanitized = sanitized.Substring(0, MaxLength) + "...[truncated]";
            }

            return sanitized;
        }

        /// <summary>
        /// Sanitizes an object for safe logging.
        /// </summary>
        /// <param name="value">The object to sanitize</param>
        /// <returns>A sanitized string representation safe for logging</returns>
        public static string Sanitize(object? value)
        {
            return Sanitize(value?.ToString());
        }
    }
}