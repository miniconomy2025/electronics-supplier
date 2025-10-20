using System.Text.Json;

using esAPI.Data;
using esAPI.DTOs;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace esAPI.Controllers;

[ApiController]
[Route("inventory")]
public class InventoryController(AppDbContext context) : ControllerBase
{
    private readonly AppDbContext _context = context;

    [HttpGet]
    [ProducesResponseType(typeof(InventorySummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetInventorySummary()
    {
        try
        {
            await using (var connection = _context.Database.GetDbConnection() as NpgsqlConnection)
            {
                await connection!.OpenAsync();

                await using (var command = new NpgsqlCommand("SELECT get_inventory_summary()", connection))
                {
                    var jsonResult = await command.ExecuteScalarAsync();

                    if (jsonResult is string jsonString)
                    {
                        var summary = JsonSerializer.Deserialize<InventorySummaryDto>(jsonString);
                        return Ok(summary);
                    }
                }
            }

            return NotFound("Could not retrieve inventory summary.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return StatusCode(500, "An error occurred while fetching the inventory summary.");
        }
    }
}
