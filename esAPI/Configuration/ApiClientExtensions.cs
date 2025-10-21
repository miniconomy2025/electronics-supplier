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

            // Configure HTTP clients with simulation-appropriate timeouts (2 min = 1 day)
            services.AddHttpClient("commercial-bank", client =>
            {
                client.BaseAddress = new Uri(externalApiConfig.CommercialBank);
                client.DefaultRequestHeaders.Add("Client-Id", externalApiConfig.ClientId);
                client.Timeout = TimeSpan.FromSeconds(30); // 30 second timeout
            });

            services.AddHttpClient("bulk-logistics", client =>
            {
                client.BaseAddress = new Uri(externalApiConfig.BulkLogistics);
                client.DefaultRequestHeaders.Add("Client-Id", externalApiConfig.ClientId);
                client.Timeout = TimeSpan.FromSeconds(30); // 30 second timeout
            });

            // Configure THOH client with simulation-appropriate timeout and optional SSL certificate validation bypass
            var thohClientBuilder = services.AddHttpClient("thoh", client =>
            {
                client.BaseAddress = new Uri(externalApiConfig.THOH);
                client.DefaultRequestHeaders.Add("Client-Id", externalApiConfig.ClientId);
                client.Timeout = TimeSpan.FromSeconds(45); // 45 second timeout for THOH
            });

            if (externalApiConfig.BypassSslValidation)
            {
                Console.WriteLine("⚠️  SSL validation bypass enabled for THOH API");
                thohClientBuilder.ConfigurePrimaryHttpMessageHandler(() =>
                {
                    return new HttpClientHandler()
                    {
                        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                    };
                });
            }

            // Configure Recycler client with simulation-appropriate timeout and optional SSL certificate validation bypass
            var recyclerClientBuilder = services.AddHttpClient("recycler", client =>
            {
                client.BaseAddress = new Uri(externalApiConfig.Recycler);
                client.DefaultRequestHeaders.Add("Client-Id", externalApiConfig.ClientId);
                client.Timeout = TimeSpan.FromSeconds(30); // 30 second timeout
            });

            if (externalApiConfig.BypassSslValidation)
            {
                Console.WriteLine("⚠️  SSL validation bypass enabled for Recycler API");
                recyclerClientBuilder.ConfigurePrimaryHttpMessageHandler(() =>
                {
                    return new HttpClientHandler()
                    {
                        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                    };
                });
            }

            return services;
        }
    }
}
