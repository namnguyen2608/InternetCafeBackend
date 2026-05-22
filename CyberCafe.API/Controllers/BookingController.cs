using System.Security.Claims;
using CyberCafe.Core.DTOs.Booking;
using CyberCafe.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CyberCafe.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]   // All booking endpoints require a valid JWT
public class BookingController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    /// <summary>
    /// Starts a session on the specified computer for the user.
    /// Customers can only start their own session; Admins/Staff can start sessions for anyone.
    /// </summary>
    [HttpPost("start")]
    [ProducesResponseType(typeof(SessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> StartSession([FromBody] StartSessionRequest request)
    {
        var currentUserId = GetUserId();
        var currentUserRole = GetUserRole();

        // Enforce security boundaries
        if (currentUserRole != "Admin" && currentUserRole != "Staff" && currentUserId != request.UserId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, 
                new SessionResponse(false, "Forbidden: You are not authorized to start a session for another user."));
        }

        var result = await _bookingService.StartSessionAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Ends the active session on the specified computer for the user.
    /// Customers can only end their own session; Admins/Staff can end sessions for anyone.
    /// </summary>
    [HttpPost("end")]
    [ProducesResponseType(typeof(SessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> EndSession([FromBody] EndSessionRequest request)
    {
        var currentUserId = GetUserId();
        var currentUserRole = GetUserRole();

        // Enforce security boundaries
        if (currentUserRole != "Admin" && currentUserRole != "Staff" && currentUserId != request.UserId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, 
                new SessionResponse(false, "Forbidden: You are not authorized to end a session for another user."));
        }

        var result = await _bookingService.EndSessionAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
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
