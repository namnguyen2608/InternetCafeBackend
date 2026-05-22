namespace CyberCafe.Core.DTOs.Account;

public record TopUpResponse(
    bool    Success,
    decimal NewBalance,
    string  Message
);
