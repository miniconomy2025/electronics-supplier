using SimulationModel = esAPI.Models.Simulation;

namespace esAPI.Services
{
    public interface ISimulationStateService
    {
        bool IsRunning { get; }
        DateTime? StartTimeUtc { get; }
        int CurrentDay { get; }
        
        void Start();
        void Stop();
        void AdvanceDay();
        decimal GetCurrentSimulationTime(int precision = 3);
        void RestoreFromBackup(SimulationModel sim);
        SimulationModel ToBackupEntity();
    }
} 