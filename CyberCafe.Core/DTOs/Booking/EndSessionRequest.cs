namespace CyberCafe.Core.DTOs.Booking;

public record EndSessionRequest(
    int UserId,
    int ComputerId
);
