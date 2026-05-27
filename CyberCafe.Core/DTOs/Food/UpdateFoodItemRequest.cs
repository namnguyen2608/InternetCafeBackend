using System.ComponentModel.DataAnnotations;

namespace CyberCafe.Core.DTOs.Food;

public record UpdateFoodItemRequest(
    [MaxLength(200)] string? Name,
    string? Description,
    [Range(0.01, 10_000_000)] decimal? Price,
    [MaxLength(100)] string? Category,
    bool? IsAvailable,
    string? ImageUrl
);
