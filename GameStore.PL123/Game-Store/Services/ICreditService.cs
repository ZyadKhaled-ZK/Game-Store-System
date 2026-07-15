namespace GameStore.PL.Services;

public class CreditEntry
{
    public decimal Amount { get; set; }
    public string Type { get; set; } = string.Empty;  // "refund", "reserved", "purchase"
    public string Reason { get; set; } = string.Empty;
    public string? StripeSessionId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class CreditLedger
{
    public List<CreditEntry> Entries { get; set; } = new();
}

public interface ICreditService
{
    Task<decimal> GetBalanceAsync(string userId);
    Task<decimal> GetAvailableBalanceAsync(string userId);
    Task<List<CreditEntry>> GetEntriesAsync(string userId);
    Task AddCreditAsync(string userId, decimal amount, string reason);
    Task<(bool Success, string Message)> ReserveAsync(string userId, decimal amount, string stripeSessionId);
    Task<(bool Success, string Message)> ConfirmReservationAsync(string userId, string stripeSessionId, string reason);
    Task ReleaseReservationAsync(string userId, string stripeSessionId);
}
