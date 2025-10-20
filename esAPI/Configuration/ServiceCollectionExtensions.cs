using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Amazon.SQS;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using esAPI.Data;
using esAPI.Clients;
using esAPI.Services;
using esAPI.Interfaces;
using esAPI.Interfaces.Services;
using esAPI.Middleware;
using esAPI.Simulation;

namespace esAPI.Configuration
{
    public static class ServiceCollectionExtensions
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

        public static IServiceCollection AddDatabaseContext(this IServiceCollection services, IConfiguration configuration)
        {
            // Build database connection string with environment variable support
            var connectionString = configuration.GetConnectionString("DefaultConnection")!;
            var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "postgres";
            connectionString += dbPassword;

            Console.WriteLine($"🗄️ Database: Connecting to {connectionString.Replace(dbPassword, "***")}");

            var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
            var dataSource = dataSourceBuilder.Build();

            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(dataSource)
            );

            return services;
        }

        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // External API clients
            services.AddScoped<IBankService, BankService>();
            services.AddScoped<IBankAccountService, BankAccountService>();
            services.AddScoped<IThohApiClient, ThohApiClient>();
            services.AddScoped<IRecyclerApiClient, RecyclerApiClient>();
            services.AddScoped<ICommercialBankClient, CommercialBankClient>();
            services.AddScoped<IBulkLogisticsClient, BulkLogisticsClient>();
            services.AddScoped<CommercialBankClient>();
            services.AddScoped<BulkLogisticsClient>();

            // Core services
            services.AddScoped<IElectronicsService, ElectronicsService>();
            services.AddScoped<ISupplyService, SupplyService>();
            services.AddScoped<IMaterialOrderService, MaterialOrderService>();
            services.AddSingleton<ISimulationStateService, SimulationStateService>();
            services.AddScoped<SimulationStartupService>();

            // Business logic services
            services.AddScoped<IMachineAcquisitionService, MachineAcquisitionService>();
            services.AddScoped<IMaterialAcquisitionService, MaterialAcquisitionService>();
            services.AddScoped<IProductionService, ProductionService>();
            services.AddScoped<Interfaces.Services.IStartupCostCalculator, StartupCostCalculator>();

            // Management services
            services.AddScoped<InventoryManagementService>();
            services.AddScoped<InventoryService>();
            services.AddScoped<MachineManagementService>();
            services.AddScoped<MaterialOrderingService>();
            services.AddScoped<SimulationDayService>();
            services.AddScoped<SimulationDayOrchestrator>();
            services.AddScoped<OrderExpirationService>();
            services.AddScoped<ElectronicsMachineDetailsService>();

            // Simulation Engine
            services.AddScoped<SimulationEngine>();

            // Retry services (optional - depends on AWS being available)
            services.TryAddScoped<RetryQueuePublisher>();

            return services;
        }

        public static IServiceCollection AddAwsServices(this IServiceCollection services)
        {
            services.AddAWSService<IAmazonSQS>();
            return services;
        }

        public static IServiceCollection AddBackgroundServices(this IServiceCollection services)
        {
            services.AddHostedService<SimulationAutoAdvanceService>();
            services.AddHostedService<OrderExpirationBackgroundService>();
            services.AddHostedService<RetryJobDispatcher>();
            return services;
        }

        public static IServiceCollection AddHealthChecksConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHealthChecks()
                .AddCheck("self", () => HealthCheckResult.Healthy("Application is running"))
                .AddCheck("External-APIs", () =>
                {
                    // Basic connectivity check - could be enhanced
                    return HealthCheckResult.Healthy("External APIs configured");
                }, tags: new[] { "external", "api" });

            return services;
        }

        public static IServiceCollection AddCorsConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            var corsConfig = new CorsConfig();
            configuration.GetSection(CorsConfig.SectionName).Bind(corsConfig);

            services.AddCors(options =>
            {
                options.AddPolicy("SecureCorsPolicy", policy =>
                {
                    var origins = corsConfig.AllowedOrigins.Length > 0
                        ? corsConfig.AllowedOrigins
                        : new[] { "http://localhost:3000", "https://localhost:7000" }; // Fallback for development

                    policy.WithOrigins(origins)
                          .WithMethods(corsConfig.AllowedMethods)
                          .WithHeaders(corsConfig.AllowedHeaders);

                    if (corsConfig.AllowCredentials)
                    {
                        policy.AllowCredentials();
                    }
                });
            });

            return services;
        }
    }
}
