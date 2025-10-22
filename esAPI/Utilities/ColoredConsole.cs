using System;

namespace esAPI.Utilities
{
    /// <summary>
    /// Utility class for writing colored console output
    /// </summary>
    public static class ColoredConsole
    {
        /// <summary>
        /// Writes an error message in red
        /// </summary>
        public static void WriteError(string message)
        {
            WriteColored(message, ConsoleColor.Red);
        }

        /// <summary>
        /// Writes a warning message in yellow
        /// </summary>
        public static void WriteWarning(string message)
        {
            WriteColored(message, ConsoleColor.Yellow);
        }

        /// <summary>
        /// Writes an information message in white
        /// </summary>
        public static void WriteInfo(string message)
        {
            WriteColored(message, ConsoleColor.White);
        }

        /// <summary>
        /// Writes a success message in green
        /// </summary>
        public static void WriteSuccess(string message)
        {
            WriteColored(message, ConsoleColor.Green);
        }

        /// <summary>
        /// Writes a debug message in gray
        /// </summary>
        public static void WriteDebug(string message)
        {
            WriteColored(message, ConsoleColor.Gray);
        }

        /// <summary>
        /// Writes a message in the specified color
        /// </summary>
        public static void WriteColored(string message, ConsoleColor color)
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