using System.Text.Json;
using esAPI.Clients;
using esAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace esAPI.Services
{
    public class BankAccountRetryHandler : IRetryHandler<BankAccountRetryJob>
    {
        private readonly AppDbContext _db;
        private readonly ICommercialBankClient _bankClient;
        private readonly ILogger<BankAccountRetryHandler> _logger;

        public BankAccountRetryHandler(AppDbContext db, ICommercialBankClient bankClient, ILogger<BankAccountRetryHandler> logger)
        {
            _db = db;
            _bankClient = bankClient;
            _logger = logger;
        }

        public async Task<bool> HandleAsync(BankAccountRetryJob job, CancellationToken token)
        {
            var response = await _bankClient.CreateAccountAsync(new { notification_url = job.NotificationUrl });
            if (!response.IsSuccessStatusCode) return false;

            var json = await response.Content.ReadAsStringAsync();
            var parsed = JsonSerializer.Deserialize<JsonElement>(json);
            var accountNumber = parsed.GetProperty("account_number").GetString();

            var company = await _db.Companies.FirstOrDefaultAsync(c => c.CompanyId == job.CompanyId, token);
            company.BankAccountNumber = accountNumber;
            await _db.SaveChangesAsync(token);

            return true;
        }
    }

}