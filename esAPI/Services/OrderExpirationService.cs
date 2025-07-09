using esAPI.Data;
using esAPI.Models;
using esAPI.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace esAPI.Services
{
    public class OrderExpirationService : IHostedService
    {
        private readonly AppDbContext _context;
        private readonly ISimulationStateService _stateService;

        public OrderExpirationService(AppDbContext context, ISimulationStateService stateService)
        {
            _context = context;
            _stateService = stateService;
        }

        /// <summary>
        /// Reserves electronics for a pending order
        /// </summary>
        /// <param name="orderId">The order ID</param>
        /// <param name="quantity">Quantity to reserve</param>
        /// <returns>True if reservation was successful</returns>
        public async Task<bool> ReserveElectronicsForOrderAsync(int orderId, int quantity)
        {
            // Get available electronics (not sold, not reserved)
            var availableElectronics = await _context.Electronics
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

            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Frees reserved electronics when an order expires
        /// </summary>
        /// <param name="orderId">The order ID</param>
        /// <returns>Number of electronics freed</returns>
        public async Task<int> FreeReservedElectronicsAsync(int orderId)
        {
            // Find electronics that were reserved for this order
            // Since we don't have a direct link, we'll free electronics that are reserved
            // and were reserved around the time the order was placed
            var order = await _context.ElectronicsOrders
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
                return 0;

            // Get reserved electronics that were reserved around the order time
            // This is a simplified approach - in a real system you'd have a direct link
            var reservedElectronics = await _context.Electronics
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

            await _context.SaveChangesAsync();
            return freedCount;
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

            // Find pending orders that are older than 1 day
            var expiredOrders = await _context.ElectronicsOrders
                .Where(o => o.OrderStatusId == (int)Order.Status.Pending && o.OrderedAt < oneDayAgo)
                .ToListAsync();

            int expiredCount = 0;
            foreach (var order in expiredOrders)
            {
                // Mark order as expired
                order.OrderStatusId = (int)Order.Status.Expired;
                
                // Free reserved electronics
                await FreeReservedElectronicsAsync(order.OrderId);
                
                expiredCount++;
            }

            if (expiredCount > 0)
            {
                await _context.SaveChangesAsync();
            }

            return expiredCount;
        }

        /// <summary>
        /// Gets the number of available electronics (not reserved, not sold)
        /// </summary>
        /// <returns>Count of available electronics</returns>
        public async Task<int> GetAvailableElectronicsCountAsync()
        {
            return await _context.Electronics
                .Where(e => e.SoldAt == null && e.ElectronicsStatusId == (int)Electronics.Status.Available)
                .CountAsync();
        }

        /// <summary>
        /// Gets the number of reserved electronics
        /// </summary>
        /// <returns>Count of reserved electronics</returns>
        public async Task<int> GetReservedElectronicsCountAsync()
        {
            return await _context.Electronics
                .Where(e => e.SoldAt == null && e.ElectronicsStatusId == (int)Electronics.Status.Reserved)
                .CountAsync();
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