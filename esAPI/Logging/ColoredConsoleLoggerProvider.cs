using Microsoft.Extensions.Logging;

namespace esAPI.Logging
{
    /// <summary>
    /// Provider for colored console loggers
    /// </summary>
    public class ColoredConsoleLoggerProvider : ILoggerProvider
    {
        private readonly ILoggerProvider _innerProvider;

        public ColoredConsoleLoggerProvider(ILoggerProvider innerProvider)
        {
            _innerProvider = innerProvider;
        }

        public ILogger CreateLogger(string categoryName)
        {
            var innerLogger = _innerProvider.CreateLogger(categoryName);
            return new ColoredConsoleLogger(categoryName, innerLogger);
        }

        public void Dispose()
        {
            _innerProvider?.Dispose();
        }
    }
}
