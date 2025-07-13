using esAPI.Clients;
using esAPI.Data;
using esAPI.Interfaces;
using esAPI.Models;

namespace esAPI.Services
{
    public class BankBalanceRetryHandler : IRetryHandler<BankBalanceRetryJob>
{
    private readonly AppDbContext _db;
    private readonly ICommercialBankClient _bankClient;
    private readonly ISimulationStateService _stateService;
    private readonly ILogger<BankBalanceRetryHandler> _logger;

    public BankBalanceRetryHandler(AppDbContext db, ICommercialBankClient bankClient, ISimulationStateService stateService, ILogger<BankBalanceRetryHandler> logger)
    {
        _db = db;
        _bankClient = bankClient;
        _stateService = stateService;
        _logger = logger;
    }

    public async Task<bool> HandleAsync(BankBalanceRetryJob job, CancellationToken token)
    {
        _logger.LogInformation("🔄 Processing BankBalanceRetryJob for day {Day}, attempt {Attempt}", job.SimulationDay, job.RetryAttempt);

        try
        {
            var balance = await _bankClient.GetAccountBalanceAsync();
            var simTime = _stateService.GetCurrentSimulationTime(3);
            var canonicalTimestamp = SimulationTimeService.ToCanonicalTime(simTime);

            var snapshot = new BankBalanceSnapshot
            {
                SimulationDay = job.SimulationDay,
                Balance = balance,
                Timestamp = canonicalTimestamp
            };
            _db.BankBalanceSnapshots.Add(snapshot);
            await _db.SaveChangesAsync(token);

            _logger.LogInformation("✅ Successfully stored bank balance snapshot on retry for day {Day}", job.SimulationDay);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to process BankBalanceRetryJob");
            return false; // Will retry again if needed
        }
    }
}

}