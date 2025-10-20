using SimulationModel = esAPI.Models.Simulation;

namespace esAPI.Interfaces
{
    public interface ISimulationStateService
    {
        bool IsRunning { get; }
        DateTime? StartTimeUtc { get; }
        int CurrentDay { get; }

        void Start();
        void Start(long epochStartTime);
        void Stop();
        void AdvanceDay();
        decimal GetCurrentSimulationTime(int precision = 3);
        void RestoreFromBackup(SimulationModel sim);
        SimulationModel ToBackupEntity();
    }
}