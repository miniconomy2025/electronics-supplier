using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace esAPI.Services
{
    public class OrderExpirationBackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OrderExpirationBackgroundService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);
        private CancellationTokenSource? _cts;
        private Task? _backgroundTask;
        private readonly object _lock = new();
        private bool _isRunning = false;

        public OrderExpirationBackgroundService(IServiceProvider serviceProvider, ILogger<OrderExpirationBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public void StartAsync()
        {
            lock (_lock)
            {
                if (_isRunning)
                {
                    _logger.LogWarning("Order Expiration Background Service is already running.");
                    return;
                }
                _logger.LogInformation("Order Expiration Background Service is starting.");
                _cts = new CancellationTokenSource();
                _backgroundTask = Task.Run(() => ExecuteAsync(_cts.Token));
                _isRunning = true;
            }
        }

        public void StopAsync()
        {
            lock (_lock)
            {
                if (!_isRunning)
                {
                    _logger.LogWarning("Order Expiration Background Service is not running.");
                    return;
                }
                _logger.LogInformation("Order Expiration Background Service is stopping.");
                _cts?.Cancel();
                _isRunning = false;
            }
        }

        private async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var orderExpirationService = scope.ServiceProvider.GetRequiredService<OrderExpirationService>();

                    var expiredCount = await orderExpirationService.CheckAndExpireOrdersAsync();

                    if (expiredCount > 0)
                    {
                        _logger.LogInformation("Expired {ExpiredCount} orders and freed reserved electronics", expiredCount);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while checking for expired orders");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }
            _logger.LogInformation("Order Expiration Background Service loop has stopped.");
        }
    }
}