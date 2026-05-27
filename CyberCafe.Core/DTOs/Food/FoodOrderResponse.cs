namespace CyberCafe.Core.DTOs.Food;

public record FoodOrderResponse(
    bool Success,
    string Message,
    int? OrderId = null,
    decimal? TotalAmount = null,
    decimal? CurrentBalance = null
);
