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
    public class MachinesController(AppDbContext context, ISimulationStateService stateService) : ControllerBase
    {
        private readonly AppDbContext _context = context;
        private readonly ISimulationStateService _stateService = stateService;

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
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Get the BROKEN status ID
            var brokenStatus = await _context.Set<MachineStatus>()
                .FirstOrDefaultAsync(s => s.Status == "BROKEN");

            if (brokenStatus == null)
                return BadRequest("BROKEN status not found in database.");

            // Get all machines that are currently working (STANDBY or IN_USE)
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

            if (machinesToBreak == 0)
            {
                return BadRequest("No working machines available to break.");
            }

            // Break the machines
            foreach (var machineData in workingMachines)
            {
                machineData.Machine.MachineStatusId = brokenStatus.StatusId;
            }

            // Record the disaster
            var disaster = new Disaster
            {
                BrokenAt = _stateService.GetCurrentSimulationTime(3),
                MachinesAffected = machinesToBreak
            };

            _context.Disasters.Add(disaster);
            await _context.SaveChangesAsync();

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
