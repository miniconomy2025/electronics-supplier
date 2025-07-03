using esAPI.Data;
using esAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("electronics/orders")]
public class ElectronicsOrdersController : ControllerBase
{
    private readonly AppDbContext _context;

    public ElectronicsOrdersController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] ElectronicsOrder order)
    {
        if (order == null)
            return BadRequest("Order data is required.");

        if (!ModelState.IsValid || order.ManufacturerId <= 0 || order.Amount <= 0)
            return BadRequest("Invalid order data.");

        order.OrderedAt = DateTime.UtcNow;
        _context.ElectronicsOrders.Add(order);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return StatusCode(500, "An error occurred while saving the order.");
        }

        // Use correct route parameter name
        return CreatedAtAction(nameof(GetOrderById), new { orderId = order.OrderId }, order);
    }

    [HttpGet]
    public async Task<ActionResult<List<ElectronicsOrder>>> GetAllOrders()
    {
        return await _context.ElectronicsOrders.ToListAsync();
    }

    [HttpGet("{orderId}")]
    public async Task<ActionResult<ElectronicsOrder>> GetOrderById(int orderId)
    {
        var order = await _context.ElectronicsOrders.FindAsync(orderId);

        if (order == null)
            return NotFound();

        return order;
    }

    [HttpPut("{orderId}")]
    public async Task<IActionResult> UpdateOrder(int orderId, [FromBody] ElectronicsOrder order)
    {
        if (order == null)
            return BadRequest("Order data is required.");

        if (order.OrderId != orderId || order.ManufacturerId <= 0 || order.Amount <= 0)
            return BadRequest("Invalid order data.");

        var existingOrder = await _context.ElectronicsOrders.FindAsync(orderId);
        if (existingOrder == null)
            return NotFound();

        existingOrder.ManufacturerId = order.ManufacturerId;
        existingOrder.Amount = order.Amount;
        existingOrder.OrderedAt = order.OrderedAt;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return StatusCode(500, "An error occurred while updating the order.");
        }

        return NoContent();
    }

    [HttpDelete("{orderId}")]
    public async Task<IActionResult> DeleteOrder(int orderId)
    {
        var order = await _context.ElectronicsOrders.FindAsync(orderId);
        if (order == null)
            return NotFound();

        _context.ElectronicsOrders.Remove(order);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return StatusCode(500, "An error occurred while deleting the order.");
        }

        return NoContent();
    }
}
