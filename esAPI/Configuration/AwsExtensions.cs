using Amazon.SQS;
using esAPI.Services;

namespace esAPI.Configuration
{
    public static class AwsExtensions
    {
        public static IServiceCollection AddAwsServices(this IServiceCollection services)
        {
            services.AddAWSService<IAmazonSQS>();
            return services;
        }

        public static IServiceCollection AddAwsDependentServices(this IServiceCollection services)
        {
            // Only add services that depend on AWS when AWS is available
            services.AddScoped<RetryQueuePublisher>();
            return services;
        }

        public static IServiceCollection AddAwsBackgroundServices(this IServiceCollection services)
        {
            // Background services that depend on AWS
            services.AddHostedService<RetryJobDispatcher>();
            return services;
        }

        public static IServiceCollection AddAwsServicesConditionally(this IServiceCollection services, IWebHostEnvironment environment)
        {
            if (environment.IsProduction() || environment.IsDevelopment())
            {
                try
                {
                    services.AddAwsServices();
                    services.AddAwsDependentServices();
                    services.AddAwsBackgroundServices();
                    Console.WriteLine("✅ AWS services configured");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ AWS services not available: {ex.Message}");
                    Console.WriteLine("🔧 Continuing without AWS services...");
                }
            }
            else
            {
                Console.WriteLine("🧪 Test/Container environment - skipping AWS services");
            }

            return services;
        }
    }
}