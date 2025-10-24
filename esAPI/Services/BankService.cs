using esAPI.Data;
using esAPI.Models;
using esAPI.Interfaces;
using esAPI.Clients;
using Microsoft.Extensions.Logging;
using esAPI.Logging;

namespace esAPI.Services
{
    public class BankService(AppDbContext db, ICommercialBankClient bankClient, ISimulationStateService stateService, ILogger<BankService> logger, RetryQueuePublisher? retryQueuePublisher) : IBankService
    {
        private readonly AppDbContext _db = db;
        private readonly ICommercialBankClient _bankClient = bankClient;
        private readonly ISimulationStateService _stateService = stateService;
        private readonly ILogger<BankService> _logger = logger;
        private readonly RetryQueuePublisher? _retryQueuePublisher = retryQueuePublisher;

        public async Task<decimal> GetAndStoreBalance(int simulationDay)
        {
            _logger.LogInformation("[BankService] Retrieving bank balance for simulation day {SimulationDay}", simulationDay);

            try
            {
                var balance = await _bankClient.GetAccountBalanceAsync();
                _logger.LogInformation("[BankService] Retrieved bank balance: {Balance}", balance);

                // NOTE: Bank balance snapshots disabled as they were causing errors and clogging logs
                // _logger.LogInformation("[BankService] Storing bank balance snapshot in database");
                // var snapshot = new BankBalanceSnapshot
                // {
                //     SimulationDay = simulationDay,
                //     Balance = (double)balance,
                //     Timestamp = simulationDay // Use the day number as timestamp
                // };
                // _db.BankBalanceSnapshots.Add(snapshot);
                // await _db.SaveChangesAsync();

                // _logger.LogInformation("[BankService] Bank balance snapshot stored: Day={Day}, Balance={Balance}, Timestamp={Timestamp}",
                //     simulationDay, balance, simulationDay);

                return balance;
            }
            catch (Exception ex)
            {
                _logger.LogErrorColored(ex, "[BankService] Failed to get bank balance");

                // NOTE: Retry functionality disabled as snapshots are disabled
                // var retryJob = new BankBalanceRetryJob
                // {
                //     SimulationDay = simulationDay,
                //     RetryAttempt = 0
                // };

                // if (_retryQueuePublisher != null)
                // {
                //     await _retryQueuePublisher.PublishAsync(retryJob);
                //     _logger.LogInformation("[BankService] Bank balance retry job enqueued");
                // }
                // else
                // {
                //     _logger.LogWarningColored("[BankService] Retry functionality not available, no retry job enqueued");
                // }

                // Return a sentinel value instead of throwing to allow simulation to continue
                _logger.LogWarningColored("[BankService] Returning sentinel balance value (-1) to allow simulation to continue");
                return -1m;
            }
        }
    }
}
