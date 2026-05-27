namespace CyberCafe.Core.DTOs.Food;

public record FoodItemDto(
    int Id,
    string Name,
    string? Description,
    decimal Price,
    string Category,
    bool IsAvailable,
    string? ImageUrl
);
