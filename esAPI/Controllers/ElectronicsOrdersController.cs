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
        if (order == null || order.ManufacturerId <= 0 || order.Amount <= 0)
        {
            return BadRequest("Invalid order data.");
        }

        _context.ElectronicsOrders.Add(order);
        order.OrderedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetOrderById), new { id = order.OrderId }, order);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ElectronicsOrder>>> GetAllOrders()
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
        if (order == null || order.OrderId != orderId || order.ManufacturerId <= 0 || order.Amount <= 0)
        {
            return BadRequest("Invalid order data.");
        }

        _context.Entry(order).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return NoContent();
    }

}
