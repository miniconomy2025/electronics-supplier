using Microsoft.Extensions.Logging;
using System;

namespace esAPI.Logging
{
    /// <summary>
    /// Extension methods for ILogger to provide colored console output
    /// </summary>
    public static class ColoredLoggerExtensions
    {
        /// <summary>
        /// Logs an error message and writes it to console in red
        /// </summary>
        public static void LogErrorColored<T>(this ILogger<T> logger, string message, params object[] args)
        {
            // Log normally first
            logger.LogError(message, args);
            
            // Then write to console in red for immediate visibility
            var formattedMessage = args.Length > 0 ? string.Format(message, args) : message;
            WriteToConsoleColored($"[ERROR] {formattedMessage}", ConsoleColor.Red);
        }

        /// <summary>
        /// Logs an error with exception and writes it to console in red
        /// </summary>
        public static void LogErrorColored<T>(this ILogger<T> logger, Exception exception, string message, params object[] args)
        {
            // Log normally first
            logger.LogError(exception, message, args);
            
            // Then write to console in red for immediate visibility
            var formattedMessage = args.Length > 0 ? string.Format(message, args) : message;
            WriteToConsoleColored($"[ERROR] {formattedMessage} - Exception: {exception.Message}", ConsoleColor.Red);
        }

        /// <summary>
        /// Logs a warning message and writes it to console in yellow
        /// </summary>
        public static void LogWarningColored<T>(this ILogger<T> logger, string message, params object[] args)
        {
            // Log normally first
            logger.LogWarning(message, args);
            
            // Then write to console in yellow for visibility
            var formattedMessage = args.Length > 0 ? string.Format(message, args) : message;
            WriteToConsoleColored($"[WARNING] {formattedMessage}", ConsoleColor.Yellow);
        }

        /// <summary>
        /// Logs a critical message and writes it to console in magenta
        /// </summary>
        public static void LogCriticalColored<T>(this ILogger<T> logger, string message, params object[] args)
        {
            // Log normally first
            logger.LogCritical(message, args);
            
            // Then write to console in magenta for maximum visibility
            var formattedMessage = args.Length > 0 ? string.Format(message, args) : message;
            WriteToConsoleColored($"[CRITICAL] {formattedMessage}", ConsoleColor.Magenta);
        }

        private static void WriteToConsoleColored(string message, ConsoleColor color)
        {
            var originalColor = Console.ForegroundColor;
            try
            {
                Console.ForegroundColor = color;
                Console.WriteLine(message);
            }
            finally
            {
                Console.ForegroundColor = originalColor;
            }
        }
    }
}