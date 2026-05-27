namespace CyberCafe.Core.Entities;

public class FoodItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;   // e.g. "Đồ ăn", "Nước uống", "Snack"
    public bool IsAvailable { get; set; } = true;
    public string? ImageUrl { get; set; }

    // Navigation properties
    public ICollection<FoodOrderItem> FoodOrderItems { get; set; } = new List<FoodOrderItem>();
}
