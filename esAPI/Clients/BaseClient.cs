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
    }
}