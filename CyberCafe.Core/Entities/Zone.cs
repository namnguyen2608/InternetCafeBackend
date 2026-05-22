namespace CyberCafe.Core.Entities;

public class Zone
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;           // e.g. "Standard", "VIP"
    public decimal PricePerHour { get; set; }

    // Navigation properties
    public ICollection<Computer> Computers { get; set; } = new List<Computer>();
}
