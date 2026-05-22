using CyberCafe.Core.DTOs.Booking;
using CyberCafe.Core.Entities;
using CyberCafe.Core.Enums;
using CyberCafe.Core.Interfaces;
using CyberCafe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CyberCafe.Services;

public class BookingService : IBookingService
{
    private readonly CyberCafeDbContext _db;

    public BookingService(CyberCafeDbContext db)
    {
        _db = db;
    }

    // ── StartSession ──────────────────────────────────────────────────────────
    /// <summary>
    /// Starts a game session for a user on a specific computer.
    /// Validates availability, wallet balance, active session duplication,
    /// and uses an EF Core Transaction for strict database integrity.
    /// </summary>
    public async Task<SessionResponse> StartSessionAsync(StartSessionRequest request)
    {
        using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            // --- 1. Load the computer with its zone --------------------------
            var computer = await _db.Computers
                .Include(c => c.Zone)
                .FirstOrDefaultAsync(c => c.Id == request.ComputerId);

            if (computer is null)
                return new SessionResponse(false, "Computer not found.");

            if (computer.Status != ComputerStatus.Available)
                return new SessionResponse(false, $"Computer '{computer.Name}' is not available (Status: {computer.Status}).");

            // --- 2. Check if the user exists and has a wallet ----------------
            var userExists = await _db.Users.AnyAsync(u => u.Id == request.UserId);
            if (!userExists)
                return new SessionResponse(false, "User not found.");

            var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == request.UserId);
            if (wallet is null)
                return new SessionResponse(false, "Wallet not found for this user.");

            // --- 3. Check for minimum balance (at least 1 hour's worth) ------
            if (wallet.Balance < computer.Zone.PricePerHour)
            {
                return new SessionResponse(false,
                    $"Insufficient balance. Minimum required for 1 hour: {computer.Zone.PricePerHour:N0}. " +
                    $"Current balance: {wallet.Balance:N0}.");
            }

            // --- 4. Verify user doesn't already have an active session -------
            bool alreadyActive = await _db.GameSessions
                .AnyAsync(gs => gs.UserId == request.UserId && gs.EndTime == null);

            if (alreadyActive)
                return new SessionResponse(false, "User already has an active session. End it before starting a new one.");

            // --- 5. Update computer status + create session ------------------
            computer.Status = ComputerStatus.InUse;

            var startTime = DateTime.UtcNow;
            var session = new GameSession
            {
                UserId     = request.UserId,
                ComputerId = request.ComputerId,
                StartTime  = startTime
            };
            _db.GameSessions.Add(session);

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return new SessionResponse(
                Success:         true,
                Message:         $"Session successfully started on '{computer.Name}'.",
                SessionId:       session.Id,
                StartTime:       startTime,
                CurrentBalance:  wallet.Balance
            );
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
            return new SessionResponse(false, "The computer was just booked by another process. Please try again.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return new SessionResponse(false, $"Error starting session: {ex.Message}");
        }
    }

    // ── EndSession ────────────────────────────────────────────────────────────
    /// <summary>
    /// Ends an active session, rounds up duration to the nearest 15 minutes,
    /// deducts total pro-rated cost from the wallet, logs a Transaction history,
    /// and resets the computer to Available using an EF Transaction.
    /// </summary>
    public async Task<SessionResponse> EndSessionAsync(EndSessionRequest request)
    {
        using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            // --- 1. Find the active session ----------------------------------
            var session = await _db.GameSessions
                .Include(gs => gs.Computer)
                    .ThenInclude(c => c.Zone)
                .FirstOrDefaultAsync(gs =>
                    gs.UserId     == request.UserId     &&
                    gs.ComputerId == request.ComputerId &&
                    gs.EndTime    == null);

            if (session is null)
                return new SessionResponse(false, "No active session found for this user on this computer.");

            // --- 2. Calculate duration by the exact minute -------------------
            var endTime = DateTime.UtcNow;
            var duration = endTime - session.StartTime;
            double totalMinutes = duration.TotalMinutes;

            // Enforce a minimum of 1 minute play time to prevent free zero-cost sessions
            if (totalMinutes < 1.0)
            {
                totalMinutes = 1.0;
            }

            // --- 3. Calculate pro-rated cost based exactly on minutes --------
            decimal pricePerHour = session.Computer.Zone.PricePerHour;
            decimal totalCost = Math.Round(((decimal)totalMinutes / 60m) * pricePerHour, 2);

            // --- 4. Load wallet and deduct balance ---------------------------
            var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == request.UserId);
            if (wallet is null)
                return new SessionResponse(false, "Wallet not found.");

            // Edge case: handle balance dropping below calculated cost (adjust cost to remaining balance)
            if (wallet.Balance < totalCost)
            {
                totalCost = wallet.Balance;
            }

            wallet.Balance -= totalCost;

            // --- 5. Record Transaction history -------------------------------
            var newTransaction = new Transaction
            {
                WalletId    = wallet.Id,
                Amount      = totalCost,
                Type        = TransactionType.Out,
                Date        = endTime,
                Description = $"Session #{session.Id} on {session.Computer.Name} " +
                              $"({totalMinutes:F1} mins actual pro-rated)"
            };
            _db.Transactions.Add(newTransaction);

            // --- 6. Close session + free up computer -------------------------
            session.EndTime = endTime;
            session.Computer.Status = ComputerStatus.Available;

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return new SessionResponse(
                Success:         true,
                Message:         $"Session ended. Duration: {totalMinutes:F1} mins. Cost: {totalCost:N0}.",
                SessionId:       session.Id,
                StartTime:       session.StartTime,
                EndTime:         endTime,
                TotalMinutes:     Math.Round(totalMinutes, 1),
                TotalCost:        totalCost,
                CurrentBalance:  wallet.Balance
            );
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return new SessionResponse(false, $"Error ending session: {ex.Message}");
        }
    }
}
