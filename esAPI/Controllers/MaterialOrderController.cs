using Microsoft.AspNetCore.Mvc;

using esAPI.DTOs.MaterialOrder;
using esAPI.Services;

namespace esAPI.Controllers
{
    [ApiController]
    [Route("materials/orders")]
    [Produces("application/json")]
    public class MaterialOrdersController(IMaterialOrderService service) : ControllerBase
    {
        private readonly IMaterialOrderService _service = service;

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<MaterialOrderResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<MaterialOrderResponse>>> GetAllMaterialOrders()
        {
            var orders = await _service.GetAllMaterialOrdersAsync();
            return Ok(orders);
        }

        [HttpGet("{orderId:int}", Name = "GetMaterialOrderById")]
        [ProducesResponseType(typeof(MaterialOrderResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<MaterialOrderResponse>> GetMaterialOrderById(int orderId)
        {
            var order = await _service.GetMaterialOrderByIdAsync(orderId);
            if (order == null)
                return NotFound();
            return Ok(order);
        }

        [HttpPost]
        [ProducesResponseType(typeof(MaterialOrderResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateMaterialOrder([FromBody] CreateMaterialOrderRequest request)
        {
            try
            {
                var newOrder = await _service.CreateMaterialOrderAsync(request);
                return CreatedAtAction(nameof(GetMaterialOrderById), new { orderId = newOrder.OrderId }, newOrder);
            }
            catch (Exception ex)
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
            var updated = await _service.UpdateMaterialOrderAsync(orderId, request);
            if (!updated)
                return NotFound();
            return NoContent();
        }
    }
}
