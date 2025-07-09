using Microsoft.AspNetCore.Mvc;

using esAPI.DTOs.Supply;
using esAPI.Services;

namespace esAPI.Controllers
{
    [ApiController]
    [Route("supplies")]
    public class SuppliesController(ISupplyService service) : ControllerBase
    {
        private readonly ISupplyService _service = service;

        [HttpPost]
        public async Task<IActionResult> CreateSupply([FromBody] CreateSupplyDto dto)
        {
            try
            {
                var created = await _service.CreateSupplyAsync(dto);
                return CreatedAtAction(nameof(GetSupplyById), new { id = created.SupplyId }, created);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<SupplyDto>>> GetAllSupplies()
        {
            var supplies = await _service.GetAllSuppliesAsync();
            return Ok(supplies);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<SupplyDto>> GetSupplyById(int id)
        {
            var supply = await _service.GetSupplyByIdAsync(id);
            if (supply == null)
                return NotFound();
            return Ok(supply);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSupplyById(int id)
        {
            var deleted = await _service.DeleteSupplyByIdAsync(id);
            if (!deleted)
                return NotFound($"Supply with ID {id} was not found.");
            return NoContent();
        }
    }
}
