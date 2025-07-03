using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using esAPI.Data;
using esAPI.Models;

[ApiController]
[Route("machines")]
public class MachinesController : ControllerBase
{
    private readonly AppDbContext _context;

    public MachinesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<ActionResult<Machine>> CreateMachine([FromBody] Machine machine)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        _context.Machines.Add(machine);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetMachineById), new { machineId = machine.MachineId }, machine);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Machine>>> GetMachines()
    {
        return await _context.Machines.ToListAsync();
    }

    [HttpGet("{machineId}")]
    public async Task<ActionResult<Machine>> GetMachineById(int machineId)
    {
        var machine = await _context.Machines.FindAsync(machineId);

        if (machine == null)
            return NotFound();

        return machine;
    }

    [HttpPut("{machineId}")]
    public async Task<IActionResult> UpdateMachine(int machineId, Machine machine)
    {
        if (machineId != machine.MachineId)
            return BadRequest("Machine ID in URL does not match body.");

        _context.Entry(machine).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await MachineExistsAsync(machineId))
                return NotFound();

            throw;
        }

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

    private async Task<bool> MachineExistsAsync(int machineId)
    {
        return await _context.Machines.AnyAsync(e => e.MachineId == machineId);
    }
}