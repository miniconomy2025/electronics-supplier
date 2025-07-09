
namespace esAPI.Clients
{
    public class CommercialBankClient(IHttpClientFactory factory) : BaseClient(factory, "commercial-bank")
    {
        public async Task<int> GetAccountBalance()
        {
            var response = await _client.GetAsync("/account/me/balance");
            return int.Parse(await response.Content.ReadAsStringAsync());
        }

        public async Task<string?> CreateAccountAsync()
        {
            var response = await _client.PostAsync("/account", null);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            using var doc = System.Text.Json.JsonDocument.Parse(content);
            return doc.RootElement.TryGetProperty("account_number", out var accNum) ? accNum.GetString() : null;
        }
    }
}
