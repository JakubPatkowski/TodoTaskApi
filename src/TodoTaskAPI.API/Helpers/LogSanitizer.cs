using CoreLogSanitizer = TodoTaskAPI.Core.Helpers.LogSanitizer;

namespace TodoTaskAPI.API.Helpers
{
    /// <summary>
    /// API-specific log sanitizer that extends Core functionality with ASP.NET Core types.
    /// Provides sanitization for PathString, QueryString and other HTTP-specific types.
    /// </summary>
    public static class LogSanitizer
    {
        /// <summary>
        /// Sanitizes a string value for safe logging.
        /// </summary>
        /// <param name="value">The string value to sanitize</param>
        /// <returns>A sanitized string safe for logging</returns>
        public static string Sanitize(string? value)
        {
            return CoreLogSanitizer.Sanitize(value);
        }

        /// <summary>
        /// Sanitizes an ASP.NET Core PathString for safe logging.
        /// </summary>
        /// <param name="path">The PathString to sanitize</param>
        /// <returns>A sanitized string representation of the path</returns>
        public static string Sanitize(PathString path)
        {
            return CoreLogSanitizer.Sanitize(path.Value);
        }

        /// <summary>
        /// Sanitizes an ASP.NET Core QueryString for safe logging.
        /// </summary>
        /// <param name="query">The QueryString to sanitize</param>
        /// <returns>A sanitized string representation of the query</returns>
        public static string Sanitize(QueryString query)
        {
            return CoreLogSanitizer.Sanitize(query.Value);
        }

        /// <summary>
        /// Sanitizes an object for safe logging.
        /// </summary>
        /// <param name="value">The object to sanitize</param>
        /// <returns>A sanitized string representation</returns>
        public static string Sanitize(object? value)
        {
            return CoreLogSanitizer.Sanitize(value);
        }
    }
}