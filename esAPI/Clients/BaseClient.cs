namespace esAPI.Clients
{
    public abstract class BaseClient
    {
        protected readonly IHttpClientFactory _factory;

        protected readonly HttpClient _client;

        protected BaseClient(IHttpClientFactory factory, string clientName)
        {
            _factory = factory;
            _client = _factory.CreateClient(clientName);

            // Ensure Client-Id header is set for outgoing requests
            if (!_client.DefaultRequestHeaders.Contains("Client-Id"))
            {
                _client.DefaultRequestHeaders.Add("Client-Id", "electronics-supplier");
            }
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
                    Console.WriteLine($"External API error response (Status {(int)response.StatusCode}): {errorContent}");
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
