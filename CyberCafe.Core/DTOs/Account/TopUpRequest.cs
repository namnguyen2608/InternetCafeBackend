namespace CyberCafe.Core.DTOs.Account;

public record TopUpRequest(
    int     UserId,
    decimal Amount,
    string? Note        // Optional description, e.g. "Cash top-up by staff"
);
