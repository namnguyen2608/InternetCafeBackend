using System.Security.Claims;
using CyberCafe.Core.DTOs.Food;
using CyberCafe.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CyberCafe.API.Controllers;

[ApiController]
[Route("api/food")]
[Authorize]
public class FoodController : ControllerBase
{
    private readonly IFoodService _foodService;

    public FoodController(IFoodService foodService)
    {
        _foodService = foodService;
    }

    // ── Menu (All authenticated users) ────────────────────────────────────────

    /// <summary>
    /// Returns the food menu. Admins/Staff can also see unavailable items.
    /// </summary>
    [HttpGet("menu")]
    [ProducesResponseType(typeof(IEnumerable<FoodItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMenu()
    {
        var role = GetUserRole();
        bool includeUnavailable = role is "Admin" or "Staff";
        var items = await _foodService.GetAllFoodItemsAsync(includeUnavailable);
        return Ok(items);
    }

    // ── Menu Management (Admin / Staff only) ──────────────────────────────────

    /// <summary>Creates a new food item on the menu.</summary>
    [HttpPost("items")]
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(typeof(FoodItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateFoodItem([FromBody] CreateFoodItemRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var item = await _foodService.CreateFoodItemAsync(request);
        return CreatedAtAction(nameof(GetMenu), new { id = item.Id }, item);
    }

    /// <summary>Updates an existing food item by ID.</summary>
    [HttpPut("items/{id:int}")]
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(typeof(FoodItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateFoodItem(int id, [FromBody] UpdateFoodItemRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var item = await _foodService.UpdateFoodItemAsync(id, request);
        return item is null
            ? NotFound(new { message = $"Không tìm thấy món ăn ID {id}." })
            : Ok(item);
    }

    /// <summary>Deletes a food item by ID.</summary>
    [HttpDelete("items/{id:int}")]
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteFoodItem(int id)
    {
        var deleted = await _foodService.DeleteFoodItemAsync(id);
        return deleted
            ? NoContent()
            : NotFound(new { message = $"Không tìm thấy món ăn ID {id}." });
    }

    // ── Customer Ordering ─────────────────────────────────────────────────────

    /// <summary>
    /// Places a food order for the current user.
    /// Deducts total cost from the user's wallet immediately.
    /// </summary>
    [HttpPost("orders")]
    [ProducesResponseType(typeof(FoodOrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(FoodOrderResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PlaceOrder([FromBody] PlaceFoodOrderRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetUserId();
        if (userId == 0)
            return Unauthorized(new { message = "Không xác định được người dùng." });

        var result = await _foodService.PlaceOrderAsync(userId, request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Returns all food orders placed by the current user.</summary>
    [HttpGet("orders/my")]
    [ProducesResponseType(typeof(IEnumerable<FoodOrderDetailDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyOrders()
    {
        var userId = GetUserId();
        if (userId == 0)
            return Unauthorized(new { message = "Không xác định được người dùng." });

        var orders = await _foodService.GetUserOrdersAsync(userId);
        return Ok(orders);
    }

    // ── Order Management (Admin / Staff) ─────────────────────────────────────

    /// <summary>Returns all food orders in the system (Admin/Staff only).</summary>
    [HttpGet("orders")]
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(typeof(IEnumerable<FoodOrderDetailDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllOrders()
    {
        var orders = await _foodService.GetAllOrdersAsync();
        return Ok(orders);
    }

    /// <summary>Updates the status of a food order (Admin/Staff only).</summary>
    [HttpPatch("orders/{id:int}/status")]
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(typeof(FoodOrderDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateFoodOrderStatusRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var order = await _foodService.UpdateOrderStatusAsync(id, request);
        return order is null
            ? NotFound(new { message = $"Không tìm thấy đơn hàng ID {id}." })
            : Ok(order);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private int GetUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                 ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
        return int.TryParse(claim, out var id) ? id : 0;
    }

    private string GetUserRole()
    {
        return User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
    }
}
