namespace esAPI.Services
{
    public class OrderExpirationBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<OrderExpirationBackgroundService> logger) : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider = serviceProvider;
        private readonly ILogger<OrderExpirationBackgroundService> _logger = logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1); // Check every minute

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

            _logger.LogInformation("Order Expiration Background Service is stopping.");
        }
    }
}