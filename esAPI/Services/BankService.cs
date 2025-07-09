using System;
using System.Threading.Tasks;
using esAPI.Data;
using esAPI.Models;
using esAPI.Clients;
using Microsoft.EntityFrameworkCore;

namespace esAPI.Services
{
    public class BankService
    {
        private readonly AppDbContext _db;
        private readonly CommercialBankClient _bankClient;
        private readonly ISimulationStateService _stateService;

        public BankService(AppDbContext db, CommercialBankClient bankClient, ISimulationStateService stateService)
        {
            _db = db;
            _bankClient = bankClient;
            _stateService = stateService;
        }

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