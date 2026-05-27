using System.ComponentModel.DataAnnotations;

namespace CyberCafe.Core.DTOs.Food;

public record PlaceFoodOrderRequest(
    [Required, MinLength(1)] List<OrderItemRequest> Items
);

public record OrderItemRequest(
    int FoodItemId,
    [Range(1, 100)] int Quantity
);
