namespace esAPI.Interfaces.Services
{
    public interface IProductionService
    {
        Task<(int electronicsCreated, Dictionary<string, int> materialsUsed)> ProduceElectronics();
    }
}