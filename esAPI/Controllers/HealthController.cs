using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace esAPI.Controllers;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    private readonly HealthCheckService _healthCheckService;

    public HealthController(HealthCheckService healthCheckService)
    {
        _healthCheckService = healthCheckService;
    }

    /// <summary>
    /// Health check endpoint for monitoring application status
    /// </summary>
    /// <returns>Health status of the application</returns>
    [HttpGet]
    [ProducesResponseType(typeof(HealthCheckResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HealthCheckResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Get()
    {
        var healthReport = await _healthCheckService.CheckHealthAsync();

        var response = new HealthCheckResponse
        {
            Status = healthReport.Status.ToString(),
            Duration = healthReport.TotalDuration,
            Checks = healthReport.Entries.Select(entry => new HealthCheckItem
            {
                Name = entry.Key,
                Status = entry.Value.Status.ToString(),
                Description = entry.Value.Description,
                Duration = entry.Value.Duration,
                Data = entry.Value.Data
            }).ToList()
        };

        return healthReport.Status == HealthStatus.Healthy
            ? Ok(response)
            : StatusCode(StatusCodes.Status503ServiceUnavailable, response);
    }
}

public class HealthCheckResponse
{
    public string Status { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public List<HealthCheckItem> Checks { get; set; } = new();
}

public class HealthCheckItem
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TimeSpan Duration { get; set; }
    public IReadOnlyDictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
}
