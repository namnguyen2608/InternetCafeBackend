using System.ComponentModel.DataAnnotations;

namespace CyberCafe.Core.DTOs.Food;

public record CreateFoodItemRequest(
    [Required, MaxLength(200)] string Name,
    string? Description,
    [Range(0.01, 10_000_000)] decimal Price,
    [Required, MaxLength(100)] string Category,
    bool IsAvailable = true,
    string? ImageUrl = null
);
