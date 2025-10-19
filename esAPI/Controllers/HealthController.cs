using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using esAPI.Data;

namespace esAPI.Controllers
{
    [ApiController]
    [Route("health")]
    public class HealthController(AppDbContext context) : ControllerBase
    {
        private readonly AppDbContext _context = context;

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                // Test database connection
                var canConnect = await _context.Database.CanConnectAsync();
                
                if (!canConnect)
                {
                    return StatusCode(503, new { 
                        status = "unhealthy", 
                        message = "Database connection failed" 
                    });
                }

                // Test a simple query
                var companyCount = await _context.Companies.CountAsync();
                
                return Ok(new
                {
                    status = "healthy",
                    database = "connected",
                    timestamp = DateTime.UtcNow,
                    companyCount = companyCount
                });
            }
            catch (Exception ex)
            {
                return StatusCode(503, new { 
                    status = "unhealthy", 
                    message = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        [HttpGet("database")]
        public async Task<IActionResult> GetDatabaseStatus()
        {
            try
            {
                var connectionString = _context.Database.GetDbConnection().ConnectionString;
                var canConnect = await _context.Database.CanConnectAsync();
                
                // Get some basic database info
                var companyCount = await _context.Companies.CountAsync();
                var materialCount = await _context.Materials.CountAsync();
                var machineCount = await _context.Machines.CountAsync();

                return Ok(new
                {
                    connectionString = connectionString?.Replace(connectionString?.Split(';')[2] ?? "", "Password=***"), // Hide password
                    connected = canConnect,
                    tables = new
                    {
                        companies = companyCount,
                        materials = materialCount,
                        machines = machineCount
                    },
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    error = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        [HttpGet("database/test")]
        public async Task<IActionResult> TestDatabaseConnection()
        {
            try
            {
                // Test connection
                var canConnect = await _context.Database.CanConnectAsync();
                if (!canConnect)
                {
                    return BadRequest(new { success = false, message = "Cannot connect to database" });
                }

                // Test simple queries sequentially to avoid DbContext threading issues
                var companyCount = await _context.Companies.CountAsync();
                var materialCount = await _context.Materials.CountAsync();
                var machineCount = await _context.Machines.CountAsync();
                var materialOrderCount = await _context.MaterialOrders.CountAsync();
                var machineOrderCount = await _context.MachineOrders.CountAsync();

                var results = new
                {
                    success = true,
                    message = "Database connection and queries successful",
                    tableCounts = new
                    {
                        companies = companyCount,
                        materials = materialCount,
                        machines = machineCount,
                        materialOrders = materialOrderCount,
                        machineOrders = machineOrderCount
                    },
                    timestamp = DateTime.UtcNow
                };

                return Ok(results);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    success = false, 
                    message = "Database test failed", 
                    error = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }
    }
}
