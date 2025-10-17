using System.Threading.Tasks;

namespace esAPI.Interfaces
{
    public interface ICommercialBankClient
    {
        Task<decimal> GetAccountBalanceAsync();
        Task<string?> CreateAccountAsync();
        Task<string> MakePaymentAsync(string toAccountNumber, string toBankName, decimal amount, string description);
        Task<string?> RequestLoanAsync(decimal amount);
        Task<bool> SetNotificationUrlAsync();
    }
}


