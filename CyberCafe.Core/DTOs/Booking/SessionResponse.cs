namespace CyberCafe.Core.DTOs.Booking;

public record SessionResponse(
    bool Success,
    string Message,
    int? SessionId = null,
    DateTime? StartTime = null,
    DateTime? EndTime = null,
    double? TotalMinutes = null,
    decimal? TotalCost = null,
    decimal? CurrentBalance = null
);
