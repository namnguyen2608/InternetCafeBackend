using CyberCafe.Core.DTOs.Food;

namespace CyberCafe.Core.Interfaces;

public interface IFoodService
{
    // ── Menu Management (Admin / Staff) ───────────────────────────────────────

    /// <summary>Creates a new food item on the menu.</summary>
    Task<FoodItemDto> CreateFoodItemAsync(CreateFoodItemRequest request);

    /// <summary>Updates an existing food item. Only provided fields are applied.</summary>
    Task<FoodItemDto?> UpdateFoodItemAsync(int id, UpdateFoodItemRequest request);

    /// <summary>Soft/hard deletes a food item. Returns false if not found.</summary>
    Task<bool> DeleteFoodItemAsync(int id);

    /// <summary>
    /// Returns all food items.
    /// If <paramref name="includeUnavailable"/> is false, only available items are returned.
    /// </summary>
    Task<IEnumerable<FoodItemDto>> GetAllFoodItemsAsync(bool includeUnavailable = false);

    // ── Ordering (Customer) ───────────────────────────────────────────────────

    /// <summary>
    /// Places a food order for a user.
    /// Validates items exist and are available, calculates total, deducts wallet balance,
    /// logs a Transaction, and saves FoodOrder + FoodOrderItems atomically.
    /// </summary>
    Task<FoodOrderResponse> PlaceOrderAsync(int userId, PlaceFoodOrderRequest request);

    /// <summary>Returns all orders placed by a specific user.</summary>
    Task<IEnumerable<FoodOrderDetailDto>> GetUserOrdersAsync(int userId);

    // ── Order Management (Admin / Staff) ─────────────────────────────────────

    /// <summary>Returns all food orders in the system (for admin/staff view).</summary>
    Task<IEnumerable<FoodOrderDetailDto>> GetAllOrdersAsync();

    /// <summary>Updates the status of a food order (e.g., Preparing → Delivered).</summary>
    Task<FoodOrderDetailDto?> UpdateOrderStatusAsync(int orderId, UpdateFoodOrderStatusRequest request);
}
