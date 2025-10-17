namespace esAPI.Services;

public interface IStartupCostCalculator
{
    Task<List<StartupPlan>> GenerateAllPossibleStartupPlansAsync();
}
