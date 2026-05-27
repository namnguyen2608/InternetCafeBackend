using System.ComponentModel.DataAnnotations;
using CyberCafe.Core.Enums;

namespace CyberCafe.Core.DTOs.Food;

public record UpdateFoodOrderStatusRequest(
    [Required] FoodOrderStatus Status
);
