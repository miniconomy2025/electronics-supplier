using System.Threading.Tasks;

namespace esAPI.Interfaces
{
    /// <summary>
    /// Service responsible for executing the startup sequence of the simulation
    /// </summary>
    public interface ISimulationStartupService
    {
        /// <summary>
        /// Executes the startup sequence for day 1 of the simulation
        /// </summary>
        /// <returns>True if startup was successful, false otherwise</returns>
        Task<bool> ExecuteStartupSequenceAsync();
    }
}
