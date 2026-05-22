namespace CyberCafe.Core.DTOs.Auth;

public record AuthResponse(
    bool Success,
    string? Token,
    string? Message
);
