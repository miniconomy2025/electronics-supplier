using Microsoft.AspNetCore.Mvc;

using esAPI.Interfaces;
using esAPI.DTOs.Electronics;

namespace esAPI.Controllers
{
    [ApiController]
    [Route("electronics")]
    public class ElectronicsController(IElectronicsService service) : ControllerBase
    {
        private readonly IElectronicsService _service = service;

        [HttpGet]
        public async Task<ActionResult<ElectronicsDetailsDto>> GetElectronics()
        {
            var details = await _service.GetElectronicsDetailsAsync();
            if (details == null)
                return NotFound();
            return Ok(details);
        }

        [HttpPost]
        public async Task<ActionResult<ProducedElectronicsResultDto>> ProduceElectronics()
        {
            var result = await _service.ProduceElectronicsAsync();
            return Created(string.Empty, result);
        }
    }
}