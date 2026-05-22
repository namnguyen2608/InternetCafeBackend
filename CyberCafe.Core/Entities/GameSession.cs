namespace CyberCafe.Core.Entities;

public class GameSession
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int ComputerId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }       // Null while session is active

    // Navigation properties
    public User User { get; set; } = null!;
    public Computer Computer { get; set; } = null!;
}
