using esAPI.DTOs.Startup;

namespace esAPI.Interfaces.Services
{
    public interface IStartupCostCalculator
    {
        Task<List<StartupPlan>> GenerateAllPossibleStartupPlansAsync();
    }
}
