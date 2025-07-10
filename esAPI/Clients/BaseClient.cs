
namespace esAPI.Clients
{
    public abstract class BaseClient
    {
        protected readonly IHttpClientFactory _factory;

        protected readonly HttpClient _client;

        public BaseClient(IHttpClientFactory factory, string clientName)
        {
            _factory = factory;
            _client = _factory.CreateClient(clientName);
        }

        protected async Task<TResponse?> GetAsync<TResponse>(string requestUri)
        {
            try
            {
                return await _client.GetFromJsonAsync<TResponse>(requestUri);
            }
            catch (Exception)
            {
                return default;
            }
        }

        protected async Task<TResponse?> PostAsync<TRequest, TResponse>(string requestUri, TRequest requestBody)
        {
            try
            {
                var response = await _client.PostAsJsonAsync(requestUri, requestBody);
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    
                    return default;
                }
                return await response.Content.ReadFromJsonAsync<TResponse>();
            }
            catch (Exception)
            {
                return default;
            }
        }
    }
}