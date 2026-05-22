using CyberCafe.Core.DTOs.Account;
using CyberCafe.Core.DTOs.Auth;
using CyberCafe.Core.Entities;

namespace CyberCafe.Core.Interfaces;

public interface IAccountService
{
    /// <summary>Returns all users (without password hash). Admin only.</summary>
    Task<IEnumerable<UserDto>> GetAllUsersAsync();

    /// <summary>Returns a single user by ID. Admin only.</summary>
    Task<UserDto?> GetUserByIdAsync(int id);

    /// <summary>Updates a user's username and/or role. Admin only.</summary>
    Task<AuthResponse> UpdateUserAsync(int id, UpdateUserRequest request);

    /// <summary>Deletes a user. Fails if the user has an active session. Admin only.</summary>
    Task<AuthResponse> DeleteUserAsync(int id);

    /// <summary>
    /// Top-up a customer's wallet balance.
    /// Can be called by Admin or Staff.
    /// </summary>
    Task<TopUpResponse> TopUpAsync(TopUpRequest request);

    /// <summary>
    /// Returns transaction history for a given user.
    /// Admin/Staff can query any user; Customers can only query themselves.
    /// </summary>
    Task<IEnumerable<Transaction>> GetTransactionHistoryAsync(int userId);
}
