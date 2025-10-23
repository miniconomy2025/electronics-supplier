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
                var fullUrl = _client.BaseAddress != null ? new Uri(_client.BaseAddress, requestUri).ToString() : requestUri;
                Console.WriteLine($"[BaseClient] GET Request: {fullUrl}");
                return await _client.GetFromJsonAsync<TResponse>(requestUri);
            }
            catch (Exception ex)
            {
                var fullUrl = _client.BaseAddress != null ? new Uri(_client.BaseAddress, requestUri).ToString() : requestUri;
                Console.WriteLine($"❌ [BaseClient] GET Exception for {fullUrl}: {ex.Message}");
                return default;
            }
        }

        protected async Task<TResponse?> PostAsync<TRequest, TResponse>(string requestUri, TRequest requestBody)
        {
            try
            {
                var fullUrl = _client.BaseAddress != null ? new Uri(_client.BaseAddress, requestUri).ToString() : requestUri;
                Console.WriteLine($"[BaseClient] POST Request: {fullUrl}");
                Console.WriteLine($"[BaseClient] POST Body: {System.Text.Json.JsonSerializer.Serialize(requestBody)}");
                
                var response = await _client.PostAsJsonAsync(requestUri, requestBody);
                var responseContent = await response.Content.ReadAsStringAsync();
                
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"❌ [BaseClient] External API error response for {fullUrl} (Status {(int)response.StatusCode}): {responseContent}");
                    return default;
                }
                
                Console.WriteLine($"✅ [BaseClient] External API success response for {fullUrl} (Status {(int)response.StatusCode}): {responseContent}");
                
                return System.Text.Json.JsonSerializer.Deserialize<TResponse>(responseContent, new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                var fullUrl = _client.BaseAddress != null ? new Uri(_client.BaseAddress, requestUri).ToString() : requestUri;
                Console.WriteLine($"❌ [BaseClient] External API exception for {fullUrl}: {ex.Message}");
                return default;
            }
        }

    }
}
