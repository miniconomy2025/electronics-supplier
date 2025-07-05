using esAPI.Data;
using esAPI.Dtos.ElectronicsDtos;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace esAPI.Controllers
{
    [ApiController]
    [Route("electronics")]
    public class ElectronicsController : ControllerBase
    {
        private readonly IElectronicsService _service;

        public ElectronicsController(IElectronicsService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<ElectronicsDetailsDto>> GetElectronics()
        {
            var details = await _service.GetElectronicsDetailsAsync();
            if (details == null)
                return NotFound();
            return Ok(details);
        }

        [HttpPost]
        public async Task<ActionResult<ProducedElectronicsResultDto>> ProduceElectronics([FromBody] ProduceElectronicsRequestDto request)
        {
            var result = await _service.ProduceElectronicsAsync(request?.MachineId, request?.Notes);
            return Created(string.Empty, result);
        }
    }
} 