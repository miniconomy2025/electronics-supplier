namespace esAPI.Interfaces
{
    /// <summary>
    /// Interface for bank service operations
    /// </summary>
    public interface IBankService
    {
        /// <summary>
        /// Retrieves the current bank balance and stores a snapshot in the database
        /// </summary>
        /// <param name="simulationDay">The simulation day number</param>
        /// <returns>The current bank balance, or -1 if retrieval failed</returns>
        Task<decimal> GetAndStoreBalance(int simulationDay);
    }
}
