using System.Security.Claims;
using CyberCafe.Core.DTOs.Account;
using CyberCafe.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CyberCafe.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AccountController : ControllerBase
{
    private readonly IAccountService _accountService;
    private readonly IAuthService    _authService;

    public AccountController(IAccountService accountService, IAuthService authService)
    {
        _accountService = accountService;
        _authService    = authService;
    }

    // ── User Management (Admin only) ─────────────────────────────────────────

    /// <summary>Get all registered users with wallet balances. Admin only.</summary>
    [HttpGet("users")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllUsers()
        => Ok(await _accountService.GetAllUsersAsync());

    /// <summary>Get a single user by ID. Admin only.</summary>
    [HttpGet("users/{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserById(int id)
    {
        var user = await _accountService.GetUserByIdAsync(id);
        return user is null ? NotFound() : Ok(user);
    }

    /// <summary>Update a user's username or role. Admin only.</summary>
    [HttpPut("users/{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request)
    {
        var result = await _accountService.UpdateUserAsync(id, request);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    /// <summary>Delete a user. Fails if the user has an active session. Admin only.</summary>
    [HttpDelete("users/{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var result = await _accountService.DeleteUserAsync(id);
        if (!result.Success) return Conflict(result);
        return Ok(result);
    }

    // ── Change Password (any authenticated user, changes their own password) ──

    /// <summary>
    /// Change the calling user's own password.
    /// The user ID is read from the JWT — users can only change their own password.
    /// </summary>
    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        // Extract userId from JWT sub claim
        var userIdClaim = User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
        if (!int.TryParse(userIdClaim, out int userId))
            return Unauthorized("Invalid token.");

        var result = await _authService.ChangePasswordAsync(userId, request);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    // ── Top-Up Wallet (Admin or Staff) ────────────────────────────────────────

    /// <summary>
    /// Top-up a customer's wallet balance.
    /// Admin and Staff can perform this operation.
    /// </summary>
    [HttpPost("topup")]
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(typeof(TopUpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TopUp([FromBody] TopUpRequest request)
    {
        var result = await _accountService.TopUpAsync(request);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    // ── Transaction History ───────────────────────────────────────────────────

    /// <summary>
    /// Get transaction history for a user.
    /// - Admin/Staff: can view any user's history.
    /// - Customer: can only view their own history.
    /// </summary>
    [HttpGet("users/{id:int}/transactions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTransactions(int id)
    {
        var userIdClaim = User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
        if (!int.TryParse(userIdClaim, out int requestingUserId))
            return Unauthorized("Invalid token.");

        // Customers may only see their own transactions
        bool isPrivileged = User.IsInRole("Admin") || User.IsInRole("Staff");
        if (!isPrivileged && requestingUserId != id)
            return Forbid();

        var transactions = await _accountService.GetTransactionHistoryAsync(id);
        return Ok(transactions);
    }
}
