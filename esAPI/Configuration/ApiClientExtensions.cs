using esAPI.Clients;

namespace esAPI.Configuration
{
    public static class ApiClientExtensions
    {
        public static IServiceCollection AddExternalApiClients(this IServiceCollection services, IConfiguration configuration)
        {
            var externalApiConfig = new ExternalApiConfig();
            configuration.GetSection(ExternalApiConfig.SectionName).Bind(externalApiConfig);

            // Validate configuration
            if (string.IsNullOrEmpty(externalApiConfig.CommercialBank) ||
                string.IsNullOrEmpty(externalApiConfig.BulkLogistics) ||
                string.IsNullOrEmpty(externalApiConfig.THOH) ||
                string.IsNullOrEmpty(externalApiConfig.Recycler))
            {
                throw new InvalidOperationException("External API configuration is incomplete. Please check appsettings.json or environment variables.");
            }

            // Log the configured endpoints (without sensitive info)
            Console.WriteLine("🔗 External API Configuration:");
            Console.WriteLine($"  Commercial Bank: {externalApiConfig.CommercialBank}");
            Console.WriteLine($"  Bulk Logistics: {externalApiConfig.BulkLogistics}");
            Console.WriteLine($"  THOH: {externalApiConfig.THOH}");
            Console.WriteLine($"  Recycler: {externalApiConfig.Recycler}");
            Console.WriteLine($"  Client ID: {externalApiConfig.ClientId}");

            // Configure HTTP clients
            services.AddHttpClient("commercial-bank", client =>
            {
                client.BaseAddress = new Uri(externalApiConfig.CommercialBank);
                client.DefaultRequestHeaders.Add("Client-Id", externalApiConfig.ClientId);
            });

            services.AddHttpClient("bulk-logistics", client =>
            {
                client.BaseAddress = new Uri(externalApiConfig.BulkLogistics);
                client.DefaultRequestHeaders.Add("Client-Id", externalApiConfig.ClientId);
            });

            services.AddHttpClient("thoh", client =>
            {
                client.BaseAddress = new Uri(externalApiConfig.THOH);
                client.DefaultRequestHeaders.Add("Client-Id", externalApiConfig.ClientId);
            });

            services.AddHttpClient("recycler", client =>
            {
                client.BaseAddress = new Uri(externalApiConfig.Recycler);
                client.DefaultRequestHeaders.Add("Client-Id", externalApiConfig.ClientId);
            });

            return services;
        }
    }
}
