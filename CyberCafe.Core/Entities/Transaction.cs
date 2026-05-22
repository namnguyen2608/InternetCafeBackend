using CyberCafe.Core.Enums;

namespace CyberCafe.Core.Entities;

public class Transaction
{
    public int Id { get; set; }
    public int WalletId { get; set; }
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }    // In (top-up) or Out (session deduction)
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public string? Description { get; set; }     // Optional note, e.g. "Session #42 charge"

    // Navigation properties
    public Wallet Wallet { get; set; } = null!;
}
