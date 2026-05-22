namespace CyberCafe.Core.DTOs.Booking;

public record StartSessionRequest(
    int UserId,
    int ComputerId
);
