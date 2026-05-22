namespace CyberCafe.Core.DTOs.Account;

public record UpdateUserRequest(
    string? Username,
    string? Role        // "Admin" | "Staff" | "Customer"
);
