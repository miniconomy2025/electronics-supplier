using esAPI.Data;
using esAPI.Models;
using esAPI.Clients;

namespace esAPI.Services
{
    public class BankService(AppDbContext db, ICommercialBankClient bankClient, ISimulationStateService stateService)
    {
        private readonly AppDbContext _db = db;
        private readonly ICommercialBankClient _bankClient = bankClient;
        private readonly ISimulationStateService _stateService = stateService;

        public async Task<decimal> GetAndStoreBalance(int simulationDay)
        {
            var balance = await _bankClient.GetAccountBalanceAsync();
            var simTime = _stateService.GetCurrentSimulationTime(3);
            var canonicalTimestamp = SimulationTimeService.ToCanonicalTime(simTime);
            var snapshot = new BankBalanceSnapshot
            {
                SimulationDay = simulationDay,
                Balance = balance,
                Timestamp = canonicalTimestamp
            };
            _db.BankBalanceSnapshots.Add(snapshot);
            await _db.SaveChangesAsync();
            return balance;
        }
    }
} 