using Amazon.SQS;
using Amazon.SQS.Model;
using esAPI.Services;

namespace esAPI.Configuration
{
    public static class AwsExtensions
    {
        public static IServiceCollection AddAwsServices(this IServiceCollection services)
        {
            // Register AWS services as singleton since they're used by hosted services
            services.AddSingleton<IAmazonSQS>(provider =>
            {
                try
                {
                    // Try to create SQS client - this will fail if no credentials are available
                    var sqsClient = new AmazonSQSClient();
                    Console.WriteLine("✅ AWS SQS client created successfully");
                    return sqsClient;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Failed to create AWS SQS client: {ex.Message}");
                    Console.WriteLine("🔧 Returning null SQS client - AWS functionality will be disabled");
                    return null!; // Return null - services should handle this gracefully
                }
            });

            return services;
        }

        public static IServiceCollection AddAwsDependentServices(this IServiceCollection services)
        {
            // Only add services that depend on AWS when AWS is available
            // RetryQueuePublisher is scoped since it's injected into other scoped services
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
                // Check if AWS credentials are available before registering services
                if (AreAwsCredentialsAvailable())
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
                        Console.WriteLine($"⚠️ AWS services registration failed: {ex.Message}");
                        Console.WriteLine("🔧 Continuing without AWS services...");
                        RegisterNullAwsServices(services);
                    }
                }
                else
                {
                    Console.WriteLine("⚠️ AWS credentials not available - skipping AWS services");
                    Console.WriteLine("🔧 Continuing without AWS services...");
                    RegisterNullAwsServices(services);
                }
            }
            else
            {
                Console.WriteLine("🧪 Test/Container environment - skipping AWS services");
                RegisterNullAwsServices(services);
            }

            return services;
        }

        private static bool AreAwsCredentialsAvailable()
        {
            // Check for environment variables
            var accessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
            var secretKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");

            if (!string.IsNullOrEmpty(accessKey) && !string.IsNullOrEmpty(secretKey))
            {
                Console.WriteLine("🔑 Found AWS credentials in environment variables");
                return true;
            }

            // Check for AWS profile
            try
            {
                var awsCredentialsPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".aws",
                    "credentials"
                );

                if (File.Exists(awsCredentialsPath))
                {
                    Console.WriteLine("🔑 Found AWS credentials file");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🔍 Could not check AWS credentials file: {ex.Message}");
            }

            // Check if running on EC2 with IAM role (simple check)
            try
            {
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(2);
                var task = httpClient.GetAsync("http://169.254.169.254/latest/meta-data/iam/security-credentials/");
                task.Wait(TimeSpan.FromSeconds(3));
                var response = task.Result;

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("🔑 Found EC2 IAM role credentials");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🔍 Could not check EC2 metadata service: {ex.Message}");
            }

            return false;
        }

        private static void RegisterNullAwsServices(IServiceCollection services)
        {
            // Register null services for components that depend on AWS
            services.AddSingleton<IAmazonSQS>(provider => null!);
            services.AddScoped<RetryQueuePublisher>(provider => null!);

            // Don't register the hosted service if AWS is not available
            Console.WriteLine("🔧 AWS services disabled - RetryQueuePublisher set to null, RetryJobDispatcher not registered");
        }
    }
}
