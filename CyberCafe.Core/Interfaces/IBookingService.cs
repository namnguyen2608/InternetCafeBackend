using CyberCafe.Core.DTOs.Booking;

namespace CyberCafe.Core.Interfaces;

public interface IBookingService
{
    /// <summary>
    /// Starts a game session for a user on a specific computer.
    /// Validates that the computer is Available and the user has sufficient wallet balance.
    /// Uses database transaction and handles concurrency to prevent double-booking.
    /// </summary>
    Task<SessionResponse> StartSessionAsync(StartSessionRequest request);

    /// <summary>
    /// Ends an active game session, calculates cost based on duration (rounded up to nearest 15 mins),
    /// deducts from wallet, logs a Transaction, and marks the computer as Available.
    /// </summary>
    Task<SessionResponse> EndSessionAsync(EndSessionRequest request);
}
