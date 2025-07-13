namespace esAPI.Clients
{
    public interface ICommercialBankClient
    {
        Task<decimal> GetAccountBalanceAsync();
        Task<HttpResponseMessage> CreateAccountAsync(object requestBody);
        Task<string> MakePaymentAsync(string toAccountNumber, string toBankName, decimal amount, string description);
        Task<string?> RequestLoanAsync(decimal amount);
        Task<string?> GetAccountDetailsAsync();
        Task<HttpResponseMessage> GetAccountAsync();
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



        public async Task<HttpResponseMessage> CreateAccountAsync(object requestBody)
        {
            // Console.WriteLine($"🔧 CommercialBankClient: Making POST request to /account");
            // Console.WriteLine($"🔧 CommercialBankClient: Base address: {_client.BaseAddress}");
            // Console.WriteLine($"🔧 CommercialBankClient: Full URL: {_client.BaseAddress}/account");
            // Console.WriteLine($"🔧 CommercialBankClient: Request URI: {_client.BaseAddress}/account");
            
            try
            {
                // Use the full URL directly to ensure it's correct
                var fullUrl = $"{_client.BaseAddress}/account";
                Console.WriteLine($"🔧 CommercialBankClient: Using full URL: {fullUrl}");
                
                var request = new HttpRequestMessage(HttpMethod.Post, fullUrl);
                var json = System.Text.Json.JsonSerializer.Serialize(requestBody);
                request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                
                var response = await _client.SendAsync(request);
                Console.WriteLine($"🔧 CommercialBankClient: Response status: {response.StatusCode}");
                Console.WriteLine($"🔧 CommercialBankClient: Response URL: {response.RequestMessage?.RequestUri}");
                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ CommercialBankClient: Exception during POST: {ex.GetType().Name}: {ex.Message}");
                throw;
            }
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

        public async Task<string?> GetAccountDetailsAsync()
        {
            var response = await _client.GetAsync("/account/me");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            using var doc = System.Text.Json.JsonDocument.Parse(content);
            return doc.RootElement.TryGetProperty("account_number", out var accNum) ? accNum.GetString() : null;
        }

        public async Task<HttpResponseMessage> GetAccountAsync()
        {
            return await _client.GetAsync("/account/me");
        }
    }
}
