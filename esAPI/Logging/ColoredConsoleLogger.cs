using Microsoft.Extensions.Logging;
using System;

namespace esAPI.Logging
{
    /// <summary>
    /// Custom logger that adds color formatting to console output based on log level
    /// </summary>
    public class ColoredConsoleLogger : ILogger
    {
        private readonly string _name;
        private readonly ILogger _innerLogger;

        public ColoredConsoleLogger(string name, ILogger innerLogger)
        {
            _name = name;
            _innerLogger = innerLogger;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => _innerLogger.BeginScope(state);

        public bool IsEnabled(LogLevel logLevel) => _innerLogger.IsEnabled(logLevel);

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            var message = formatter(state, exception);
            
            // Set console color based on log level
            var originalColor = Console.ForegroundColor;
            try
            {
                Console.ForegroundColor = GetLogLevelColor(logLevel);
                
                // Let the inner logger handle the actual logging
                _innerLogger.Log(logLevel, eventId, state, exception, formatter);
            }
            finally
            {
                Console.ForegroundColor = originalColor;
            }
        }

        private static ConsoleColor GetLogLevelColor(LogLevel logLevel)
        {
            return logLevel switch
            {
                LogLevel.Critical => ConsoleColor.Magenta,
                LogLevel.Error => ConsoleColor.Red,
                LogLevel.Warning => ConsoleColor.Yellow,
                LogLevel.Information => ConsoleColor.White,
                LogLevel.Debug => ConsoleColor.Gray,
                LogLevel.Trace => ConsoleColor.DarkGray,
                _ => ConsoleColor.White
            };
        }
    }
}