namespace esAPI.Interfaces
{
    /// <summary>
    /// Interface for bank account management operations
    /// </summary>
    public interface IBankAccountService
    {
        /// <summary>
        /// Sets up a bank account for the company with the commercial bank
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A tuple containing success status, account number if successful, and error message if failed</returns>
        Task<(bool Success, string? AccountNumber, string? Error)> SetupBankAccountAsync(CancellationToken cancellationToken = default);
    }
}