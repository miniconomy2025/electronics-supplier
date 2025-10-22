using Microsoft.Extensions.Diagnostics.HealthChecks;
using esAPI.Services;
using esAPI.Interfaces;
using esAPI.Interfaces.Services;
using esAPI.Simulation;
using esAPI.Clients;

namespace esAPI.Configuration
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // External API clients
            services.AddScoped<IBankService, BankService>();
            services.AddScoped<BankService>(); // Concrete class for direct injection
            services.AddScoped<IBankAccountService, BankAccountService>();
            services.AddScoped<BankAccountService>(); // Concrete class for direct injection
            services.AddScoped<IThohApiClient, ThohApiClient>();
            services.AddScoped<ThohApiClient>(); // Concrete class for direct injection
            services.AddScoped<IRecyclerApiClient, RecyclerApiClient>();
            services.AddScoped<RecyclerApiClient>(); // Concrete class for direct injection
            services.AddScoped<ISupplierApiClient, RecyclerApiClient>(); // RecyclerApiClient implements ISupplierApiClient
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
            services.AddScoped<IStartupCostCalculator, StartupCostCalculator>();

            // Management services
            services.AddScoped<InventoryManagementService>();
            services.AddScoped<InventoryService>();
            services.AddScoped<IMachineManagementService, MachineManagementService>();
            services.AddScoped<MachineManagementService>();
            services.AddScoped<IMaterialOrderingService, MaterialOrderingService>();
            services.AddScoped<MaterialOrderingService>();
            services.AddScoped<ISimulationDayService, SimulationDayService>();
            services.AddScoped<SimulationDayService>();
            services.AddScoped<SimulationDayOrchestrator>();
            services.AddScoped<OrderExpirationService>();
            services.AddScoped<ElectronicsMachineDetailsService>();

            // Material services
            services.AddScoped<IMaterialSourcingService, MaterialSourcingService>();

            // Simulation services
            services.AddScoped<ISimulationStartupService, SimulationDayStartupService>();

            // Simulation Engine
            services.AddScoped<SimulationEngine>();

            return services;
        }

        public static IServiceCollection AddBackgroundServices(this IServiceCollection services)
        {
            services.AddHostedService<SimulationAutoAdvanceService>();
            services.AddHostedService<OrderExpirationBackgroundService>();
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

        public static IServiceCollection AddServiceOptions(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<InventoryConfig>(
                configuration.GetSection(InventoryConfig.SectionName)
            );
            services.Configure<ExternalApiConfig>(
                configuration.GetSection(ExternalApiConfig.SectionName)
            );

            return services;
        }
    }
}
