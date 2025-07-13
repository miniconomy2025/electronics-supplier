using esAPI.Data;
using esAPI.Models;
using esAPI.Interfaces;
using esAPI.Clients;
using Microsoft.Extensions.Logging;

namespace esAPI.Services
{
    public class BankService(AppDbContext db, ICommercialBankClient bankClient, ISimulationStateService stateService, ILogger<BankService> logger, RetryQueuePublisher retryQueuePublisher)
    {
        private readonly AppDbContext _db = db;
        private readonly ICommercialBankClient _bankClient = bankClient;
        private readonly ISimulationStateService _stateService = stateService;
        private readonly ILogger<BankService> _logger = logger;
        private readonly RetryQueuePublisher _retryQueuePublisher = retryQueuePublisher;

        public async Task<decimal> GetAndStoreBalance(int simulationDay)
        {
            _logger.LogInformation("🏦 Retrieving bank balance for simulation day {SimulationDay}", simulationDay);

            try
            {
                var balance = await _bankClient.GetAccountBalanceAsync();
                _logger.LogInformation("💰 Retrieved bank balance: {Balance}", balance);

                _logger.LogInformation("💾 Storing bank balance snapshot in database");
                var snapshot = new BankBalanceSnapshot
                {
                    SimulationDay = simulationDay,
                    Balance = balance,
                    Timestamp = simulationDay // Use the day number as timestamp
                };
                _db.BankBalanceSnapshots.Add(snapshot);
                await _db.SaveChangesAsync();

                _logger.LogInformation("✅ Bank balance snapshot stored: Day={Day}, Balance={Balance}, Timestamp={Timestamp}",
                    simulationDay, balance, simulationDay);

                return balance;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to get or store bank balance, enqueuing retry job");

                var retryJob = new BankBalanceRetryJob
                {
                    SimulationDay = simulationDay,
                    RetryAttempt = 0
                };

                await _retryQueuePublisher.PublishAsync(retryJob);

                throw; // or return a sentinel value to indicate failure if you prefer
            }
        }
    }
}