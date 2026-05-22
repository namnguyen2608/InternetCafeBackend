using CyberCafe.Core.Enums;

namespace CyberCafe.Core.Entities;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Customer;

    // Navigation properties
    public Wallet? Wallet { get; set; }
    public ICollection<GameSession> GameSessions { get; set; } = new List<GameSession>();
}
