namespace CyberCafe.Core.DTOs.Account;

public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword
);
