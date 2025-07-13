using esAPI.Exceptions;

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

        protected async Task<TResponse> GetAsync<TResponse>(string requestUri)
        {
            try
            {
                HttpResponseMessage response = await _client.GetAsync(requestUri);

                if (!response.IsSuccessStatusCode)
                {
                    await HandleErrorResponse(response);
                }

                var result = await response.Content.ReadFromJsonAsync<TResponse>();
                return result == null
                    ? throw new ApiResponseParseException($"Failed to parse successful response from GET {requestUri}. Response body was null or invalid.")
                    : result;
            }
            catch (Exception ex) when (ex is not ApiClientException and not ApiResponseParseException)
            {
                throw;
            }
        }

        protected async Task<TResponse> PostAsync<TRequest, TResponse>(string requestUri, TRequest requestBody)
        {
            try
            {
                HttpResponseMessage response = await _client.PostAsJsonAsync(requestUri, requestBody);

                if (!response.IsSuccessStatusCode)
                {
                    await HandleErrorResponse(response);
                }

                var result = await response.Content.ReadFromJsonAsync<TResponse>();
                return result == null
                    ? throw new ApiResponseParseException($"Failed to parse successful response from POST {requestUri}. Response body was null or invalid.")
                    : result;
            }
            catch (Exception ex) when (ex is not ApiClientException and not ApiResponseParseException)
            {
                throw;
            }
        }

        protected async Task HandleErrorResponse(HttpResponseMessage response)
        {
            var statusCode = response.StatusCode;
            var content = await response.Content.ReadAsStringAsync();

            throw new ApiCallFailedException(
                $"API call failed with status code {statusCode}.",
                statusCode,
                content
            );
        }
    }
}