using CyberCafe.Core.Enums;

namespace CyberCafe.Core.DTOs.Account;

/// <summary>Safe user representation — never exposes PasswordHash.</summary>
public record UserDto(
    int      Id,
    string   Username,
    UserRole Role,
    decimal? WalletBalance
);
