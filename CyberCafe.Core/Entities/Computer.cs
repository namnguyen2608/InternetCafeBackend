using CyberCafe.Core.Enums;

namespace CyberCafe.Core.Entities;

public class Computer
{
    public int Id { get; set; }
    public int ZoneId { get; set; }
    public string Name { get; set; } = string.Empty;           // e.g. "PC-01"
    public ComputerStatus Status { get; set; } = ComputerStatus.Available;
    public string Specs { get; set; } = string.Empty;          // e.g. "Intel i7, RTX 3060, 16GB RAM"

    // Concurrency token — EF Core uses this to detect concurrent updates
    [System.ComponentModel.DataAnnotations.Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    // Navigation properties
    public Zone Zone { get; set; } = null!;
    public ICollection<GameSession> GameSessions { get; set; } = new List<GameSession>();
}
