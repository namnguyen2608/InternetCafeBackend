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

    /// <summary>Create a new zone. Admin only.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] Zone zone)
    {
        _db.Zones.Add(zone);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAll), new { id = zone.Id }, zone);
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
        _db.Computers.Add(computer);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = computer.Id }, computer);
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
}
