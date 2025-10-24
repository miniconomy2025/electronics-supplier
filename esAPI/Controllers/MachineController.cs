using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using esAPI.Data;
using esAPI.DTOs;
using esAPI.Models;
using esAPI.Interfaces;

namespace esAPI.Controllers
{
    [ApiController]
    [Route("machines")]
    public class MachinesController(AppDbContext context, ISimulationStateService stateService, ILogger<MachinesController> logger) : ControllerBase
    {
        private readonly AppDbContext _context = context;
        private readonly ISimulationStateService _stateService = stateService;
        private readonly ILogger<MachinesController> _logger = logger;

        [HttpPost]
        public async Task<ActionResult<MachineDto>> CreateMachine([FromBody] MachineDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var status = await _context.Set<MachineStatus>()
                .FirstOrDefaultAsync(s => s.Status == dto.Status);

            if (status == null)
                return BadRequest($"Status '{dto.Status}' does not exist.");

            var machine = new Machine
            {
                MachineStatusId = status.StatusId,
                PurchasePrice = dto.PurchasePrice,
                PurchasedAt = _stateService.GetCurrentSimulationTime(3)
            };

            _context.Machines.Add(machine);
            await _context.SaveChangesAsync();

            dto.MachineId = machine.MachineId;
            return CreatedAtAction(nameof(GetMachineById), new { machineId = machine.MachineId }, dto);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MachineDto>>> GetMachines()
        {
            return await _context.Machines
                .Join(_context.Set<MachineStatus>(),
                      m => m.MachineStatusId,
                      s => s.StatusId,
                      (m, s) => new MachineDto
                      {
                          MachineId = m.MachineId,
                          Status = s.Status,
                          PurchasePrice = m.PurchasePrice,
                          PurchasedAt = m.PurchasedAt
                      })
                .ToListAsync();
        }

        [HttpGet("{machineId}")]
        public async Task<ActionResult<MachineDto>> GetMachineById(int machineId)
        {
            var result = await _context.Machines
                .Where(m => m.MachineId == machineId)
                .Join(_context.Set<MachineStatus>(),
                      m => m.MachineStatusId,
                      s => s.StatusId,
                      (m, s) => new MachineDto
                      {
                          MachineId = m.MachineId,
                          Status = s.Status,
                          PurchasePrice = m.PurchasePrice,
                          PurchasedAt = m.PurchasedAt
                      })
                .FirstOrDefaultAsync();

            if (result == null)
                return NotFound();

            return result;
        }

        [HttpPut("{machineId}")]
        public async Task<IActionResult> UpdateMachine(int machineId, MachineDto dto)
        {
            if (machineId != dto.MachineId)
                return BadRequest("Machine ID in URL does not match body.");

            var machine = await _context.Machines.FindAsync(machineId);
            if (machine == null)
                return NotFound();

            var status = await _context.Set<MachineStatus>()
                .FirstOrDefaultAsync(s => s.Status == dto.Status);

            if (status == null)
                return BadRequest($"Status '{dto.Status}' does not exist.");

            machine.MachineStatusId = status.StatusId;
            machine.PurchasePrice = dto.PurchasePrice;
            machine.PurchasedAt = _stateService.GetCurrentSimulationTime(3);

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{machineId}")]
        public async Task<IActionResult> DeleteMachine(int machineId)
        {
            var machine = await _context.Machines.FindAsync(machineId);
            if (machine == null)
                return NotFound();

            _context.Machines.Remove(machine);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("failure")]
        public async Task<ActionResult<DisasterDto>> ReportMachineFailure([FromBody] MachineFailureDto dto)
        {
            _logger.LogInformation("[Machine-Failure] POST /machines/failure endpoint called");
            _logger.LogInformation("[Machine-Failure] Request body: FailureQuantity={FailureQuantity}", dto?.FailureQuantity ?? 0);

            if (dto == null)
            {
                _logger.LogWarning("[Machine-Failure] Request body is null");
                return BadRequest("Request body cannot be null");
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("[Machine-Failure] Invalid model state: {ModelState}", string.Join(", ", ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
                return BadRequest(ModelState);
            }

            // Get the BROKEN status ID
            _logger.LogInformation("[Machine-Failure] Looking up BROKEN status in database");
            var brokenStatus = await _context.Set<MachineStatus>()
                .FirstOrDefaultAsync(s => s.Status == "BROKEN");

            if (brokenStatus == null)
            {
                _logger.LogError("[Machine-Failure] BROKEN status not found in database");
                return BadRequest("BROKEN status not found in database.");
            }

            _logger.LogInformation("[Machine-Failure] Found BROKEN status with ID: {StatusId}", brokenStatus.StatusId);

            // Get all machines that are currently working (STANDBY or IN_USE)
            _logger.LogInformation("[Machine-Failure] Querying for working machines (STANDBY or IN_USE) to break {RequestedQuantity} machines", dto.FailureQuantity);
            var workingMachines = await _context.Machines
                .Join(_context.Set<MachineStatus>(),
                      m => m.MachineStatusId,
                      s => s.StatusId,
                      (m, s) => new { Machine = m, Status = s })
                .Where(x => x.Status.Status == "STANDBY" || x.Status.Status == "IN_USE")
                .OrderBy(x => x.Machine.MachineId) // Consistent ordering for predictable selection
                .Take(dto.FailureQuantity)
                .ToListAsync();

            var machinesToBreak = workingMachines.Count;
            _logger.LogInformation("[Machine-Failure] Found {AvailableToBreak} working machines to break (requested: {RequestedQuantity})", machinesToBreak, dto.FailureQuantity);

            if (machinesToBreak == 0)
            {
                _logger.LogWarning("[Machine-Failure] No working machines available to break");
                return BadRequest("No working machines available to break.");
            }

            // Break the machines
            _logger.LogInformation("[Machine-Failure] Updating {MachineCount} machines to BROKEN status", machinesToBreak);
            var brokenMachineIds = new List<int>();
            
            foreach (var machineData in workingMachines)
            {
                _logger.LogDebug("[Machine-Failure] Breaking machine ID {MachineId} (was {PreviousStatus})", 
                    machineData.Machine.MachineId, machineData.Status.Status);
                
                machineData.Machine.MachineStatusId = brokenStatus.StatusId;
                brokenMachineIds.Add(machineData.Machine.MachineId);
            }

            // Record the disaster
            var currentTime = _stateService.GetCurrentSimulationTime(3);
            _logger.LogInformation("[Machine-Failure] Recording disaster at simulation time {SimulationTime} affecting {MachineCount} machines", 
                currentTime, machinesToBreak);
            
            var disaster = new Disaster
            {
                BrokenAt = currentTime,
                MachinesAffected = machinesToBreak
            };

            _context.Disasters.Add(disaster);
            
            _logger.LogInformation("[Machine-Failure] Saving changes to database (machine status updates and disaster record)");
            await _context.SaveChangesAsync();

            _logger.LogInformation("[Machine-Failure] Successfully completed machine failure process. Disaster ID: {DisasterId}, Broken machine IDs: [{MachineIds}]", 
                disaster.DisasterId, string.Join(", ", brokenMachineIds));

            // Return the disaster information
            var disasterDto = new DisasterDto
            {
                DisasterId = disaster.DisasterId,
                BrokenAt = disaster.BrokenAt,
                MachinesAffected = disaster.MachinesAffected
            };

            return CreatedAtAction(nameof(GetMachineById), new { machineId = 0 }, disasterDto);
        }
    }
}
