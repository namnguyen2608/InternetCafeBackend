namespace CyberCafe.Core.DTOs.Food;

public record FoodOrderDetailDto(
    int OrderId,
    int UserId,
    string Username,
    DateTime OrderTime,
    decimal TotalAmount,
    string Status,
    List<FoodOrderItemDetailDto> Items
);

public record FoodOrderItemDetailDto(
    int FoodItemId,
    string FoodItemName,
    int Quantity,
    decimal UnitPrice,
    decimal SubTotal
);
