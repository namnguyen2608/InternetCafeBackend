using CyberCafe.Core.Entities;
using CyberCafe.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CyberCafe.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ZonesController : ControllerBase
{
    private readonly CyberCafeDbContext _db;

    public ZonesController(CyberCafeDbContext db) => _db = db;

    /// <summary>Get all zones with their pricing.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await _db.Zones.AsNoTracking().ToListAsync());

    /// <summary>Get a single zone by ID.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var zone = await _db.Zones
            .Include(z => z.Computers)
            .AsNoTracking()
            .FirstOrDefaultAsync(z => z.Id == id);
        return zone is null ? NotFound() : Ok(zone);
    }

    /// <summary>Create a new zone. Admin only.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] Zone zone)
    {
        _db.Zones.Add(zone);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = zone.Id }, zone);
    }

    /// <summary>Update a zone's name or price per hour. Admin only.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] Zone updated)
    {
        var zone = await _db.Zones.FindAsync(id);
        if (zone is null) return NotFound();

        if (!string.IsNullOrWhiteSpace(updated.Name))
            zone.Name = updated.Name;

        if (updated.PricePerHour > 0)
            zone.PricePerHour = updated.PricePerHour;

        await _db.SaveChangesAsync();
        return Ok(zone);
    }

    /// <summary>Delete a zone. Fails with 409 if the zone still has computers. Admin only.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var zone = await _db.Zones
            .Include(z => z.Computers)
            .FirstOrDefaultAsync(z => z.Id == id);

        if (zone is null) return NotFound();

        if (zone.Computers.Any())
            return Conflict($"Cannot delete zone '{zone.Name}': it still has {zone.Computers.Count} computer(s). Remove or reassign them first.");

        _db.Zones.Remove(zone);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

[ApiController]
[Route("api/[controller]")]
public class ComputersController : ControllerBase
{
    private readonly CyberCafeDbContext _db;

    public ComputersController(CyberCafeDbContext db) => _db = db;

    /// <summary>Get all computers with their zone and current status.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await _db.Computers
                        .Include(c => c.Zone)
                        .AsNoTracking()
                        .ToListAsync());

    /// <summary>Get a single computer by ID.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var computer = await _db.Computers
            .Include(c => c.Zone)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);

        return computer is null ? NotFound() : Ok(computer);
    }

    /// <summary>Add a new computer to a zone. Admin only.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] Computer computer)
    {
        var zoneExists = await _db.Zones.AnyAsync(z => z.Id == computer.ZoneId);
        if (!zoneExists)
            return BadRequest($"Zone with ID {computer.ZoneId} does not exist.");

        _db.Computers.Add(computer);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = computer.Id }, computer);
    }

    /// <summary>Update a computer's name, specs, or zone. Admin only.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] Computer updated)
    {
        var computer = await _db.Computers.FindAsync(id);
        if (computer is null) return NotFound();

        if (!string.IsNullOrWhiteSpace(updated.Name))
            computer.Name = updated.Name;

        if (!string.IsNullOrWhiteSpace(updated.Specs))
            computer.Specs = updated.Specs;

        if (updated.ZoneId > 0 && updated.ZoneId != computer.ZoneId)
        {
            var zoneExists = await _db.Zones.AnyAsync(z => z.Id == updated.ZoneId);
            if (!zoneExists)
                return BadRequest($"Zone with ID {updated.ZoneId} does not exist.");
            computer.ZoneId = updated.ZoneId;
        }

        await _db.SaveChangesAsync();
        return Ok(computer);
    }

    /// <summary>Update a computer's status (e.g., set to Maintenance). Admin/Staff only.</summary>
    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] string status)
    {
        var computer = await _db.Computers.FindAsync(id);
        if (computer is null) return NotFound();

        if (!Enum.TryParse<CyberCafe.Core.Enums.ComputerStatus>(status, ignoreCase: true, out var parsed))
            return BadRequest($"Invalid status '{status}'. Valid values: Available, InUse, Maintenance.");

        computer.Status = parsed;
        await _db.SaveChangesAsync();
        return Ok(computer);
    }

    /// <summary>Delete a computer. Fails with 409 if it has an active session. Admin only.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var computer = await _db.Computers.FindAsync(id);
        if (computer is null) return NotFound();

        bool hasActiveSession = await _db.GameSessions
            .AnyAsync(gs => gs.ComputerId == id && gs.EndTime == null);

        if (hasActiveSession)
            return Conflict($"Cannot delete computer '{computer.Name}': it has an active session in progress.");

        _db.Computers.Remove(computer);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
