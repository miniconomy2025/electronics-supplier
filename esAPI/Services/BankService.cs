using esAPI.Data;
using esAPI.Models;
using esAPI.Interfaces;
using esAPI.Clients;
using Microsoft.Extensions.Logging;

namespace esAPI.Services
{
    public class BankService(AppDbContext db, ICommercialBankClient bankClient, ISimulationStateService stateService, ILogger<BankService> logger)
    {
        private readonly AppDbContext _db = db;
        private readonly ICommercialBankClient _bankClient = bankClient;
        private readonly ISimulationStateService _stateService = stateService;
        private readonly ILogger<BankService> _logger = logger;

        public async Task<decimal> GetAndStoreBalance(int simulationDay)
        {
            _logger.LogInformation("🏦 Retrieving bank balance for simulation day {SimulationDay}", simulationDay);
            
            var balance = await _bankClient.GetAccountBalanceAsync();
            _logger.LogInformation("💰 Retrieved bank balance: {Balance}", balance);
            
            var simTime = _stateService.GetCurrentSimulationTime(3);
            var canonicalTimestamp = SimulationTimeService.ToCanonicalTime(simTime);
            
            _logger.LogInformation("💾 Storing bank balance snapshot in database");
            var snapshot = new BankBalanceSnapshot
            {
                SimulationDay = simulationDay,
                Balance = balance,
                Timestamp = canonicalTimestamp
            };
            _db.BankBalanceSnapshots.Add(snapshot);
            await _db.SaveChangesAsync();
            
            _logger.LogInformation("✅ Bank balance snapshot stored: Day={Day}, Balance={Balance}, Timestamp={Timestamp}", 
                simulationDay, balance, canonicalTimestamp);
            
            return balance;
        }
    }
}