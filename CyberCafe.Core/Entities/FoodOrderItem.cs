namespace CyberCafe.Core.Entities;

public class FoodOrderItem
{
    public int Id { get; set; }
    public int FoodOrderId { get; set; }
    public int FoodItemId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }   // Snapshot giá tại thời điểm đặt

    // Navigation properties
    public FoodOrder FoodOrder { get; set; } = null!;
    public FoodItem FoodItem { get; set; } = null!;
}
