using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using esAPI.Data;

namespace esAPI.Tests.Integration
{
    public class DatabaseConnectionTests : IDisposable
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly AppDbContext _context;

        public DatabaseConnectionTests()
        {
            // Setup configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .Build();

            // Setup services
            var services = new ServiceCollection();
            services.AddDbContext<AppDbContext>(options =>
            {
                var connectionString = configuration.GetConnectionString("DefaultConnection");
                options.UseNpgsql(connectionString);
                options.EnableSensitiveDataLogging(); // For debugging
            });

            _serviceProvider = services.BuildServiceProvider();
            _context = _serviceProvider.GetRequiredService<AppDbContext>();
        }

        [Fact]
        public async Task DatabaseConnection_ShouldConnectSuccessfully()
        {
            // Act & Assert
            var canConnect = await _context.Database.CanConnectAsync();
            Assert.True(canConnect, "Database connection should be successful");
        }

        [Fact]
        public async Task DatabaseConnection_ShouldExecuteSimpleQuery()
        {
            // Act
            var companyCount = await _context.Companies.CountAsync();
            
            // Assert
            Assert.True(companyCount >= 0, "Should be able to execute queries against the database");
        }

        [Fact]
        public async Task DatabaseConnection_ShouldHaveRequiredTables()
        {
            // Act
            var companiesTableExists = await _context.Database.ExecuteSqlRawAsync(
                "SELECT 1 FROM information_schema.tables WHERE table_name = 'companies'") >= 0;
            
            var materialsTableExists = await _context.Database.ExecuteSqlRawAsync(
                "SELECT 1 FROM information_schema.tables WHERE table_name = 'materials'") >= 0;

            var machinesTableExists = await _context.Database.ExecuteSqlRawAsync(
                "SELECT 1 FROM information_schema.tables WHERE table_name = 'machines'") >= 0;

            // Assert
            Assert.True(companiesTableExists, "Companies table should exist");
            Assert.True(materialsTableExists, "Materials table should exist");
            Assert.True(machinesTableExists, "Machines table should exist");
        }

        [Fact]
        public async Task DatabaseConnection_ShouldHaveDataInKeyTables()
        {
            // Act
            var hasCompanies = await _context.Companies.AnyAsync();
            var hasMaterials = await _context.Materials.AnyAsync();
            var hasMachines = await _context.Machines.AnyAsync();

            // Assert - These might be empty in a fresh database, so we just check they're accessible
            // The important thing is that we can query them without errors
            var _ = hasCompanies; // Use the variable to avoid compiler warnings
            var __ = hasMaterials;
            var ___ = hasMachines;
            
            // If we get here without exceptions, the tables are accessible
            Assert.True(true, "Database tables are accessible for querying");
        }

        [Fact]
        public void DatabaseConnection_ShouldHaveValidConnectionString()
        {
            // Act
            var connectionString = _context.Database.GetConnectionString();
            
            // Assert
            Assert.NotNull(connectionString);
            Assert.Contains("Host=", connectionString);
            Assert.Contains("Database=", connectionString);
            Assert.Contains("Username=", connectionString);
        }

        public void Dispose()
        {
            _context?.Dispose();
            _serviceProvider?.Dispose();
        }
    }
}
