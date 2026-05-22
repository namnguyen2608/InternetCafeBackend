using CyberCafe.Core.DTOs.Account;
using CyberCafe.Core.DTOs.Auth;
using CyberCafe.Core.Entities;
using CyberCafe.Core.Enums;
using CyberCafe.Core.Interfaces;
using CyberCafe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CyberCafe.Services;

public class AccountService : IAccountService
{
    private readonly CyberCafeDbContext _db;

    public AccountService(CyberCafeDbContext db) => _db = db;

    // ── Get All Users ─────────────────────────────────────────────────────────
    public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
    {
        return await _db.Users
            .AsNoTracking()
            .Include(u => u.Wallet)
            .Select(u => new UserDto(u.Id, u.Username, u.Role, u.Wallet != null ? u.Wallet.Balance : (decimal?)null))
            .ToListAsync();
    }

    // ── Get User By ID ────────────────────────────────────────────────────────
    public async Task<UserDto?> GetUserByIdAsync(int id)
    {
        var user = await _db.Users
            .AsNoTracking()
            .Include(u => u.Wallet)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user is null) return null;

        return new UserDto(user.Id, user.Username, user.Role, user.Wallet?.Balance);
    }

    // ── Update User ───────────────────────────────────────────────────────────
    public async Task<AuthResponse> UpdateUserAsync(int id, UpdateUserRequest request)
    {
        var user = await _db.Users.FindAsync(id);
        if (user is null)
            return new AuthResponse(false, null, "User not found.");

        // Update username if provided
        if (!string.IsNullOrWhiteSpace(request.Username) && request.Username != user.Username)
        {
            bool taken = await _db.Users.AnyAsync(u => u.Username == request.Username && u.Id != id);
            if (taken)
                return new AuthResponse(false, null, $"Username '{request.Username}' is already taken.");
            user.Username = request.Username;
        }

        // Update role if provided
        if (!string.IsNullOrWhiteSpace(request.Role))
        {
            if (!Enum.TryParse<UserRole>(request.Role, ignoreCase: true, out var newRole))
                return new AuthResponse(false, null, $"Invalid role '{request.Role}'. Valid values: Admin, Staff, Customer.");
            user.Role = newRole;
        }

        await _db.SaveChangesAsync();
        return new AuthResponse(true, null, $"User '{user.Username}' updated successfully.");
    }

    // ── Delete User ───────────────────────────────────────────────────────────
    public async Task<AuthResponse> DeleteUserAsync(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user is null)
            return new AuthResponse(false, null, "User not found.");

        // Prevent deletion if the user has an active session
        bool hasActiveSession = await _db.GameSessions
            .AnyAsync(gs => gs.UserId == id && gs.EndTime == null);

        if (hasActiveSession)
            return new AuthResponse(false, null, "Cannot delete user: they have an active session in progress.");

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
        return new AuthResponse(true, null, $"User '{user.Username}' deleted successfully.");
    }

    // ── Top-Up ────────────────────────────────────────────────────────────────
    public async Task<TopUpResponse> TopUpAsync(TopUpRequest request)
    {
        if (request.Amount <= 0)
            return new TopUpResponse(false, 0, "Top-up amount must be greater than zero.");

        var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == request.UserId);
        if (wallet is null)
            return new TopUpResponse(false, 0, "Wallet not found for this user.");

        wallet.Balance += request.Amount;

        _db.Transactions.Add(new Transaction
        {
            WalletId    = wallet.Id,
            Amount      = request.Amount,
            Type        = TransactionType.In,
            Date        = DateTime.UtcNow,
            Description = request.Note ?? $"Top-up of {request.Amount:N0}"
        });

        await _db.SaveChangesAsync();
        return new TopUpResponse(true, wallet.Balance, $"Successfully added {request.Amount:N0}. New balance: {wallet.Balance:N0}.");
    }

    // ── Transaction History ───────────────────────────────────────────────────
    public async Task<IEnumerable<Transaction>> GetTransactionHistoryAsync(int userId)
    {
        var wallet = await _db.Wallets
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.UserId == userId);

        if (wallet is null) return Enumerable.Empty<Transaction>();

        return await _db.Transactions
            .AsNoTracking()
            .Where(t => t.WalletId == wallet.Id)
            .OrderByDescending(t => t.Date)
            .ToListAsync();
    }
}
