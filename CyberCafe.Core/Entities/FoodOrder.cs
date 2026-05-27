using CyberCafe.Core.Enums;

namespace CyberCafe.Core.Entities;

public class FoodOrder
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTime OrderTime { get; set; } = DateTime.UtcNow;
    public decimal TotalAmount { get; set; }
    public FoodOrderStatus Status { get; set; } = FoodOrderStatus.Pending;

    // Navigation properties
    public User User { get; set; } = null!;
    public ICollection<FoodOrderItem> FoodOrderItems { get; set; } = new List<FoodOrderItem>();
}
