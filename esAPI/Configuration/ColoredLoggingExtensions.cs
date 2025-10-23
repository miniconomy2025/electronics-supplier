using Microsoft.Extensions.Logging;
using esAPI.Logging;

namespace esAPI.Configuration
{
    /// <summary>
    /// Extension methods for configuring colored console logging
    /// </summary>
    public static class ColoredLoggingExtensions
    {
        /// <summary>
        /// Adds colored console logging to the logging builder
        /// </summary>
        public static ILoggingBuilder AddColoredConsole(this ILoggingBuilder builder)
        {
            return builder.AddConsole(options =>
            {
                options.FormatterName = "colored";
            });
        }

        /// <summary>
        /// Configures logging with colored output for simulation services
        /// </summary>
        public static ILoggingBuilder AddSimulationLogging(this ILoggingBuilder builder)
        {
            return builder
                .ClearProviders()
                .AddConsole()
                .SetMinimumLevel(LogLevel.Information);
        }
    }
}