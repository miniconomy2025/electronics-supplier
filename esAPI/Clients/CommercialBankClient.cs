
namespace esAPI.Clients
{
    public class CommercialBankClient(IHttpClientFactory factory) : BaseClient(factory, "commercial-bank")
    {
        public async Task<int> GetAccountBalance()
        {
            var response = await _client.GetAsync("/account/me/balance");
            return int.Parse(await response.Content.ReadAsStringAsync());
        }
    }
}
