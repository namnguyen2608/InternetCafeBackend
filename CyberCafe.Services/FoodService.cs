using CyberCafe.Core.DTOs.Food;
using CyberCafe.Core.Entities;
using CyberCafe.Core.Enums;
using CyberCafe.Core.Interfaces;
using CyberCafe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CyberCafe.Services;

public class FoodService : IFoodService
{
    private readonly CyberCafeDbContext _db;

    public FoodService(CyberCafeDbContext db)
    {
        _db = db;
    }

    // ── Menu Management ───────────────────────────────────────────────────────

    public async Task<IEnumerable<FoodItemDto>> GetAllFoodItemsAsync(bool includeUnavailable = false)
    {
        var query = _db.FoodItems.AsQueryable();

        if (!includeUnavailable)
            query = query.Where(f => f.IsAvailable);

        return await query
            .OrderBy(f => f.Category)
            .ThenBy(f => f.Name)
            .Select(f => MapToFoodItemDto(f))
            .ToListAsync();
    }

    public async Task<FoodItemDto> CreateFoodItemAsync(CreateFoodItemRequest request)
    {
        var item = new FoodItem
        {
            Name        = request.Name,
            Description = request.Description,
            Price       = request.Price,
            Category    = request.Category,
            IsAvailable = request.IsAvailable,
            ImageUrl    = request.ImageUrl
        };

        _db.FoodItems.Add(item);
        await _db.SaveChangesAsync();

        return MapToFoodItemDto(item);
    }

    public async Task<FoodItemDto?> UpdateFoodItemAsync(int id, UpdateFoodItemRequest request)
    {
        var item = await _db.FoodItems.FindAsync(id);
        if (item is null) return null;

        if (request.Name        is not null) item.Name        = request.Name;
        if (request.Description is not null) item.Description = request.Description;
        if (request.Price       is not null) item.Price       = request.Price.Value;
        if (request.Category    is not null) item.Category    = request.Category;
        if (request.IsAvailable is not null) item.IsAvailable = request.IsAvailable.Value;
        if (request.ImageUrl    is not null) item.ImageUrl    = request.ImageUrl;

        await _db.SaveChangesAsync();

        return MapToFoodItemDto(item);
    }

    public async Task<bool> DeleteFoodItemAsync(int id)
    {
        var item = await _db.FoodItems.FindAsync(id);
        if (item is null) return false;

        _db.FoodItems.Remove(item);
        await _db.SaveChangesAsync();
        return true;
    }

    // ── Customer Ordering ─────────────────────────────────────────────────────

    public async Task<FoodOrderResponse> PlaceOrderAsync(int userId, PlaceFoodOrderRequest request)
    {
        using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            // --- 1. Validate user and wallet ---------------------------------
            var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
            if (wallet is null)
                return new FoodOrderResponse(false, "Không tìm thấy ví của người dùng.");

            // --- 2. Validate all requested food items ------------------------
            var foodItemIds = request.Items.Select(i => i.FoodItemId).Distinct().ToList();
            var foodItems = await _db.FoodItems
                .Where(f => foodItemIds.Contains(f.Id))
                .ToDictionaryAsync(f => f.Id);

            foreach (var item in request.Items)
            {
                if (!foodItems.TryGetValue(item.FoodItemId, out var foodItem))
                    return new FoodOrderResponse(false, $"Món ăn ID {item.FoodItemId} không tồn tại.");

                if (!foodItem.IsAvailable)
                    return new FoodOrderResponse(false, $"Món '{foodItem.Name}' hiện không có sẵn.");
            }

            // --- 3. Calculate total amount -----------------------------------
            decimal totalAmount = request.Items.Sum(i =>
                foodItems[i.FoodItemId].Price * i.Quantity);

            // --- 4. Check wallet balance -------------------------------------
            if (wallet.Balance < totalAmount)
                return new FoodOrderResponse(false,
                    $"Số dư ví không đủ. Cần: {totalAmount:N0}đ, Hiện có: {wallet.Balance:N0}đ.");

            // --- 5. Create FoodOrder -----------------------------------------
            var order = new FoodOrder
            {
                UserId      = userId,
                OrderTime   = DateTime.UtcNow,
                TotalAmount = totalAmount,
                Status      = FoodOrderStatus.Pending
            };
            _db.FoodOrders.Add(order);
            await _db.SaveChangesAsync(); // Get order.Id

            // --- 6. Create FoodOrderItems ------------------------------------
            var orderItems = request.Items.Select(i => new FoodOrderItem
            {
                FoodOrderId = order.Id,
                FoodItemId  = i.FoodItemId,
                Quantity    = i.Quantity,
                UnitPrice   = foodItems[i.FoodItemId].Price  // Snapshot giá
            }).ToList();

            _db.FoodOrderItems.AddRange(orderItems);

            // --- 7. Deduct wallet balance ------------------------------------
            wallet.Balance -= totalAmount;

            // --- 8. Log transaction ------------------------------------------
            _db.Transactions.Add(new Transaction
            {
                WalletId    = wallet.Id,
                Amount      = totalAmount,
                Type        = TransactionType.Out,
                Date        = order.OrderTime,
                Description = $"Đặt đồ ăn - Đơn #{order.Id} ({request.Items.Count} loại món)"
            });

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return new FoodOrderResponse(
                Success:        true,
                Message:        $"Đặt hàng thành công! Đơn #{order.Id} đang chờ xử lý.",
                OrderId:        order.Id,
                TotalAmount:    totalAmount,
                CurrentBalance: wallet.Balance
            );
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return new FoodOrderResponse(false, $"Lỗi khi đặt hàng: {ex.Message}");
        }
    }

    public async Task<IEnumerable<FoodOrderDetailDto>> GetUserOrdersAsync(int userId)
    {
        return await _db.FoodOrders
            .Include(fo => fo.User)
            .Include(fo => fo.FoodOrderItems)
                .ThenInclude(foi => foi.FoodItem)
            .Where(fo => fo.UserId == userId)
            .OrderByDescending(fo => fo.OrderTime)
            .Select(fo => MapToOrderDetailDto(fo))
            .ToListAsync();
    }

    // ── Admin / Staff ─────────────────────────────────────────────────────────

    public async Task<IEnumerable<FoodOrderDetailDto>> GetAllOrdersAsync()
    {
        return await _db.FoodOrders
            .Include(fo => fo.User)
            .Include(fo => fo.FoodOrderItems)
                .ThenInclude(foi => foi.FoodItem)
            .OrderByDescending(fo => fo.OrderTime)
            .Select(fo => MapToOrderDetailDto(fo))
            .ToListAsync();
    }

    public async Task<FoodOrderDetailDto?> UpdateOrderStatusAsync(int orderId, UpdateFoodOrderStatusRequest request)
    {
        var order = await _db.FoodOrders
            .Include(fo => fo.User)
            .Include(fo => fo.FoodOrderItems)
                .ThenInclude(foi => foi.FoodItem)
            .FirstOrDefaultAsync(fo => fo.Id == orderId);

        if (order is null) return null;

        order.Status = request.Status;
        await _db.SaveChangesAsync();

        return MapToOrderDetailDto(order);
    }

    // ── Mappers ───────────────────────────────────────────────────────────────

    private static FoodItemDto MapToFoodItemDto(FoodItem f) => new(
        f.Id, f.Name, f.Description, f.Price, f.Category, f.IsAvailable, f.ImageUrl);

    private static FoodOrderDetailDto MapToOrderDetailDto(FoodOrder fo) => new(
        OrderId:     fo.Id,
        UserId:      fo.UserId,
        Username:    fo.User.Username,
        OrderTime:   fo.OrderTime,
        TotalAmount: fo.TotalAmount,
        Status:      fo.Status.ToString(),
        Items:       fo.FoodOrderItems.Select(foi => new FoodOrderItemDetailDto(
            FoodItemId:   foi.FoodItemId,
            FoodItemName: foi.FoodItem.Name,
            Quantity:     foi.Quantity,
            UnitPrice:    foi.UnitPrice,
            SubTotal:     foi.UnitPrice * foi.Quantity
        )).ToList()
    );
}
