namespace CyberCafe.Core.DTOs.Auth;

public record RegisterRequest(
    string Username,
    string Password,
    string Role = "Customer"   // Defaults to Customer; Admin/Staff set explicitly
);
