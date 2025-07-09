using esAPI.Data;
using esAPI.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace esAPI.Services
{
    public class OrderExpirationService(IServiceProvider serviceProvider, ISimulationStateService stateService) : IHostedService
    {
        private readonly IServiceProvider _serviceProvider = serviceProvider;
        private readonly ISimulationStateService _stateService = stateService;

        /// <summary>
        /// Reserves electronics for a pending order
        /// </summary>
        /// <param name="orderId">The order ID</param>
        /// <param name="quantity">Quantity to reserve</param>
        /// <returns>True if reservation was successful</returns>
        public async Task<bool> ReserveElectronicsForOrderAsync(int orderId, int quantity)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                // Get available electronics (not sold, not reserved)
                var availableElectronics = await db.Electronics
                    .Where(e => e.SoldAt == null && e.ElectronicsStatusId == (int)Electronics.Status.Available)
                    .OrderBy(e => e.ProducedAt) // FIFO - oldest first
                    .Take(quantity)
                    .ToListAsync();

                if (availableElectronics.Count < quantity)
                {
                    return false; // Not enough available electronics
                }

                // Mark electronics as reserved
                foreach (var electronic in availableElectronics)
                {
                    electronic.ElectronicsStatusId = (int)Electronics.Status.Reserved;
                }

                await db.SaveChangesAsync();
                return true;
            }
        }

        /// <summary>
        /// Frees reserved electronics when an order expires
        /// </summary>
        /// <param name="orderId">The order ID</param>
        /// <returns>Number of electronics freed</returns>
        public async Task<int> FreeReservedElectronicsAsync(int orderId)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                // Find electronics that were reserved for this order
                // Since we don't have a direct link, we'll free electronics that are reserved
                // and were reserved around the time the order was placed
                var order = await db.ElectronicsOrders
                    .FirstOrDefaultAsync(o => o.OrderId == orderId);

                if (order == null)
                    return 0;

                // Get reserved electronics that were reserved around the order time
                // This is a simplified approach - in a real system you'd have a direct link
                var reservedElectronics = await db.Electronics
                    .Where(e => e.ElectronicsStatusId == (int)Electronics.Status.Reserved && e.SoldAt == null)
                    .OrderBy(e => e.ProducedAt)
                    .Take(order.TotalAmount) // Free up to the total order amount
                    .ToListAsync();

                int freedCount = 0;
                foreach (var electronic in reservedElectronics)
                {
                    electronic.ElectronicsStatusId = (int)Electronics.Status.Available;
                    freedCount++;
                }

                await db.SaveChangesAsync();
                return freedCount;
            }
        }

        /// <summary>
        /// Checks for and expires orders that are older than 1 simulation day
        /// </summary>
        /// <returns>Number of orders expired</returns>
        public async Task<int> CheckAndExpireOrdersAsync()
        {
            if (!_stateService.IsRunning)
                return 0;

            var currentTime = _stateService.GetCurrentSimulationTime(3);
            var oneDayAgo = currentTime - 1.0m; // 1 simulation day ago

            using (var scope = _serviceProvider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                // Get current electronics price per unit
                var pricePerUnit = db.LookupValues
                    .OrderByDescending(l => l.ChangedAt)
                    .Select(l => l.ElectronicsPricePerUnit)
                    .FirstOrDefault();

                // Find pending orders that are older than 1 day
                var expiredOrders = await db.ElectronicsOrders
                    .Where(o => o.OrderStatusId == (int)Order.Status.Pending && o.OrderedAt < oneDayAgo)
                    .ToListAsync();

                int expiredCount = 0;
                foreach (var order in expiredOrders)
                {
                    // Calculate total due for the order
                    var totalDue = order.TotalAmount * pricePerUnit;
                    // Sum all successful payments for this order
                    var totalPaid = db.Payments
                        .Where(p => p.OrderId == order.OrderId && p.Status == "SUCCESS")
                        .Sum(p => (decimal?)p.Amount) ?? 0m;
                    // Only expire if not fully paid
                    if (totalPaid < totalDue)
                    {
                        // Mark order as expired
                        order.OrderStatusId = (int)Order.Status.Expired;
                        // Free reserved electronics
                        await FreeReservedElectronicsAsync(order.OrderId);
                        expiredCount++;
                    }
                }
                if (expiredCount > 0)
                {
                    await db.SaveChangesAsync();
                }
                return expiredCount;
            }
        }

        /// <summary>
        /// Gets the number of available electronics (not reserved, not sold)
        /// </summary>
        /// <returns>Count of available electronics</returns>
        public async Task<int> GetAvailableElectronicsCountAsync()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                return await db.Electronics
                    .Where(e => e.SoldAt == null && e.ElectronicsStatusId == (int)Electronics.Status.Available)
                    .CountAsync();
            }
        }

        /// <summary>
        /// Gets the number of reserved electronics
        /// </summary>
        /// <returns>Count of reserved electronics</returns>
        public async Task<int> GetReservedElectronicsCountAsync()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                return await db.Electronics
                    .Where(e => e.SoldAt == null && e.ElectronicsStatusId == (int)Electronics.Status.Reserved)
                    .CountAsync();
            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // TODO: Add background logic here
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            // TODO: Add cleanup logic here
            return Task.CompletedTask;
        }
    }
} 