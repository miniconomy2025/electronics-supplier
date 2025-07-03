using System.Data;
using System.Text.Json;
using esAPI.Data;
using esAPI.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

[ApiController]
[Route("materials/orders")]
[Produces("application/json")]
public class MaterialOrdersController : ControllerBase
{
    private readonly AppDbContext _context;

    public MaterialOrdersController(AppDbContext context)
    {
        _context = context;
    }


    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<MaterialOrderResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<MaterialOrderResponse>>> GetAllMaterialOrders()
    {
        var orders = await _context.MaterialOrders
             .Include(o => o.Supplier)
             .Include(o => o.Items)
                 .ThenInclude(i => i.Material)
             .OrderByDescending(o => o.OrderedAt)
             .Select(o => new MaterialOrderResponse
             {
                 OrderId = o.OrderId,
                 SupplierId = o.SupplierId,
                 SupplierName = o.Supplier!.SupplierName,
                 OrderedAt = o.OrderedAt,
                 ReceivedAt = o.ReceivedAt,
                 Status = o.ReceivedAt == null ? "PENDING" : "COMPLETED",
                 Items = o.Items.Select(i => new MaterialOrderItemResponse
                 {
                     MaterialId = i.MaterialId,
                     MaterialName = i.Material!.MaterialName,
                     Amount = i.Amount
                 }).ToList()
             })
             .ToListAsync();

        return Ok(orders);
    }


    [HttpGet("{orderId:int}", Name = "GetMaterialOrderById")]
    [ProducesResponseType(typeof(MaterialOrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MaterialOrderResponse>> GetMaterialOrderById(int orderId)
    {
        var order = await _context.MaterialOrders
            .Where(o => o.OrderId == orderId)
            .Include(o => o.Supplier)
            .Include(o => o.Items)
                .ThenInclude(i => i.Material)
            .Select(o => new MaterialOrderResponse
            {
                OrderId = o.OrderId,
                SupplierId = o.SupplierId,
                SupplierName = o.Supplier!.SupplierName,
                OrderedAt = o.OrderedAt,
                ReceivedAt = o.ReceivedAt,
                Status = o.ReceivedAt == null ? "PENDING" : "COMPLETED",
                Items = o.Items.Select(i => new MaterialOrderItemResponse
                {
                    MaterialId = i.MaterialId,
                    MaterialName = i.Material!.MaterialName,
                    Amount = i.Amount
                }).ToList()
            })
            .SingleOrDefaultAsync();

        if (order == null)
        {
            return NotFound();
        }

        return Ok(order);
    }


    [HttpPost]
    [ProducesResponseType(typeof(MaterialOrderResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateMaterialOrder([FromBody] CreateMaterialOrderRequest request)
    {
        var itemsJson = JsonSerializer.Serialize(request.Items);
        var createdOrderIdParam = new NpgsqlParameter("p_created_order_id", DbType.Int32)
        {
            Direction = ParameterDirection.InputOutput,
            Value = DBNull.Value 
        };

        try
        {
            await _context.Database.ExecuteSqlRawAsync(
                "CALL create_material_order_with_items(@p_supplier_id, @p_items::jsonb, @p_created_order_id)",
                new NpgsqlParameter("p_supplier_id", request.SupplierId),
                new NpgsqlParameter("p_items", itemsJson),
                createdOrderIdParam
            );

            if (createdOrderIdParam.Value is int newOrderId)
            {
                var newOrderResult = await GetMaterialOrderById(newOrderId);
                return CreatedAtAction(nameof(GetMaterialOrderById), new { orderId = newOrderId }, newOrderResult.Value);
            }
            else
            {
                throw new InvalidOperationException("Could not retrieve the new order ID after creation.");
            }
        }
        catch (PostgresException ex)
        {
            return BadRequest(new { Error = "An error occurred while processing the order.", Details = ex.Message });
        }
    }


    [HttpPut("{orderId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMaterialOrder(int orderId, [FromBody] UpdateMaterialOrderRequest request)
    {
        try
        {
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"CALL update_material_order({orderId}, {request.SupplierId}, {request.OrderedAt}, {request.ReceivedAt})");

            return NoContent();
        }
        catch (PostgresException ex)
        {
            if (ex.Message.Contains("does not exist"))
            {
                return NotFound(new { Error = ex.Message });
            }
            return BadRequest(new { Error = ex.Message });
        }
    }
}