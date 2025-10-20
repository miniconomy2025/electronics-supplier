using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace esAPI.Services
{
    public class OrderExpirationBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OrderExpirationBackgroundService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);

        public OrderExpirationBackgroundService(IServiceProvider serviceProvider, ILogger<OrderExpirationBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Order Expiration Background Service is starting.");

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
            
            _logger.LogInformation("Order Expiration Background Service has stopped.");
        }
    }
}