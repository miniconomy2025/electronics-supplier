using Microsoft.EntityFrameworkCore;
using Npgsql;
using esAPI.Data;

namespace esAPI.Configuration
{
    public static class DatabaseExtensions
    {
        public static IServiceCollection AddDatabaseContext(this IServiceCollection services, IConfiguration configuration)
        {
            // Get the connection string from configuration (environment variables take precedence)
            var connectionString = configuration.GetConnectionString("DefaultConnection")!;

            Console.WriteLine($"🗄️ Database: Connecting to {MaskPassword(connectionString)}");

            var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
            // Map the PostgreSQL enum to the C# enum
            dataSourceBuilder.MapEnum<esAPI.Models.Enums.PickupRequest.PickupType>("request_type");
            var dataSource = dataSourceBuilder.Build();

            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(dataSource)
            );

            return services;
        }

        private static string MaskPassword(string connectionString)
        {
            // Simple password masking for logging
            var parts = connectionString.Split(';');
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].StartsWith("Password=", StringComparison.OrdinalIgnoreCase))
                {
                    parts[i] = "Password=***";
                    break;
                }
            }
            return string.Join(';', parts);
        }
    }
}
