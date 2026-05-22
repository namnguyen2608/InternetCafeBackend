namespace CyberCafe.Core.DTOs.Auth;

public record LoginRequest(
    string Username,
    string Password
);
