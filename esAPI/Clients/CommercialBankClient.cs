namespace esAPI.Clients
{
    public interface ICommercialBankClient
    {
        Task<decimal> GetAccountBalanceAsync();
        Task<string?> CreateAccountAsync();
        Task<string> MakePaymentAsync(string toAccountNumber, string toBankName, decimal amount, string description);
        Task<string?> RequestLoanAsync(decimal amount);

        Task<bool> SetNotificationUrlAsync();
    }

    public class CommercialBankClient(IHttpClientFactory factory) : ICommercialBankClient
    {
        private readonly IHttpClientFactory _factory = factory;
        private readonly HttpClient _client = factory.CreateClient("commercial-bank");

        public async Task<decimal> GetAccountBalanceAsync()
        {
            var response = await _client.GetAsync("/account/me/balance");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            using var doc = System.Text.Json.JsonDocument.Parse(content);
            return doc.RootElement.TryGetProperty("balance", out var bal) ? bal.GetDecimal() : 0m;
        }

        public async Task<bool> SetNotificationUrlAsync()
        {
            var requestBody = new { notification_url = "https://electronics-supplier-api.projects.bbdgrad.com/payments" };

            var response = await _client.PostAsJsonAsync("/account/me/notify", requestBody);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            using var doc = System.Text.Json.JsonDocument.Parse(content);
            return doc.RootElement.TryGetProperty("success", out var bal) && bal.GetBoolean();
        }

        public async Task<string?> CreateAccountAsync()
        {
            var response = await _client.PostAsync("/account", null);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            using var doc = System.Text.Json.JsonDocument.Parse(content);
            return doc.RootElement.TryGetProperty("account_number", out var accNum) ? accNum.GetString() : null;
        }

        public async Task<string?> RequestLoanAsync(decimal amount)
        {
            var requestBody = new { Amount = amount };

            var response = await _client.PostAsJsonAsync("/loan", requestBody);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            using var doc = System.Text.Json.JsonDocument.Parse(content);
            return doc.RootElement.TryGetProperty("loan_number", out var loanNum) ? loanNum.GetString() : null;;
        }

        public async Task<string> MakePaymentAsync(string toAccountNumber, string toBankName, decimal amount, string description)
        {
            var paymentReq = new
            {
                to_account_number = toAccountNumber,
                to_bank_name = toBankName,
                amount,
                description
            };
            var response = await _client.PostAsJsonAsync("/transaction", paymentReq);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            using var doc = System.Text.Json.JsonDocument.Parse(content);
            var root = doc.RootElement;
            if (!root.TryGetProperty("success", out var successProp) || !successProp.GetBoolean())
            {
                var errorMsg = root.TryGetProperty("error", out var err) ? err.GetString() : "Unknown payment error";
                throw new Exception($"Payment failed: {errorMsg}");
            }
            return root.TryGetProperty("transaction_number", out var txn) ? txn.GetString() ?? string.Empty : string.Empty;
        }
    }
}
