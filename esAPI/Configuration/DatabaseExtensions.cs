using Microsoft.EntityFrameworkCore;
using Npgsql;
using esAPI.Data;

namespace esAPI.Configuration
{
    public static class DatabaseExtensions
    {
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
    }
}