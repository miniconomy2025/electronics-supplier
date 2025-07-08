using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using esAPI.Data;
using esAPI.Models;
using esAPI.Services;

[ApiController]
[Route("machines")]
public class MachinesController(AppDbContext context, SimulationStateService stateService) : ControllerBase
{
    private readonly AppDbContext _context = context;
    private readonly SimulationStateService _stateService = stateService;

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
}