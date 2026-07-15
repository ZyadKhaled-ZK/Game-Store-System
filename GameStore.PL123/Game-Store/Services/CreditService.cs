namespace GameStore.PL.Services;

public class CreditService : ICreditService
{
    private readonly IJsonFileStore _fileStore;
    private const string Dir = "credits";

    public CreditService(IJsonFileStore fileStore)
    {
        _fileStore = fileStore;
    }

    private string FilePath(string userId) => $"{Dir}/{userId}.json";

    private async Task<CreditLedger> GetLedgerAsync(string userId)
    {
        return await _fileStore.ReadAsync<CreditLedger>(FilePath(userId))
            ?? new CreditLedger();
    }

    private Task WriteLedgerAsync(string userId, CreditLedger ledger)
    {
        return _fileStore.WriteAsync(FilePath(userId), ledger);
    }

    public async Task<decimal> GetBalanceAsync(string userId)
    {
        var ledger = await GetLedgerAsync(userId);
        return ledger.Entries
            .Where(e => e.Type != "reserved")
            .Sum(e => e.Amount);
    }

    public async Task<decimal> GetAvailableBalanceAsync(string userId)
    {
        var ledger = await GetLedgerAsync(userId);
        return ledger.Entries.Sum(e => e.Amount);
    }

    public async Task<List<CreditEntry>> GetEntriesAsync(string userId)
    {
        var ledger = await GetLedgerAsync(userId);
        return ledger.Entries.OrderByDescending(e => e.CreatedAt).ToList();
    }

    public async Task AddCreditAsync(string userId, decimal amount, string reason)
    {
        var ledger = await GetLedgerAsync(userId);
        ledger.Entries.Add(new CreditEntry
        {
            Amount = amount,
            Type = "refund",
            Reason = reason,
            CreatedAt = DateTime.UtcNow
        });
        await WriteLedgerAsync(userId, ledger);
    }

    public async Task<(bool Success, string Message)> ReserveAsync(string userId, decimal amount, string stripeSessionId)
    {
        var available = await GetAvailableBalanceAsync(userId);
        if (available < amount)
            return (false, $"Insufficient store credit. Available: ${available:F2}, needed: ${amount:F2}.");

        var ledger = await GetLedgerAsync(userId);
        ledger.Entries.Add(new CreditEntry
        {
            Amount = -amount,
            Type = "reserved",
            StripeSessionId = stripeSessionId,
            Reason = $"Reserved against session {stripeSessionId}",
            CreatedAt = DateTime.UtcNow
        });
        await WriteLedgerAsync(userId, ledger);
        return (true, "Credit reserved.");
    }

    public async Task<(bool Success, string Message)> ConfirmReservationAsync(string userId, string stripeSessionId, string reason)
    {
        var ledger = await GetLedgerAsync(userId);

        // Idempotency: skip if purchase entry already exists for this session
        if (ledger.Entries.Any(e => e.Type == "purchase" && e.StripeSessionId == stripeSessionId))
            return (true, "Already confirmed.");

        var reserved = ledger.Entries.FirstOrDefault(e =>
            e.Type == "reserved" && e.StripeSessionId == stripeSessionId);
        if (reserved == null)
            return (false, "Reservation not found for this session.");

        var amount = Math.Abs(reserved.Amount);
        ledger.Entries.Remove(reserved);
        ledger.Entries.Add(new CreditEntry
        {
            Amount = -amount,
            Type = "purchase",
            StripeSessionId = stripeSessionId,
            Reason = reason,
            CreatedAt = DateTime.UtcNow
        });

        await WriteLedgerAsync(userId, ledger);
        return (true, "Reservation confirmed.");
    }

    public async Task ReleaseReservationAsync(string userId, string stripeSessionId)
    {
        var ledger = await GetLedgerAsync(userId);
        var reserved = ledger.Entries.FirstOrDefault(e =>
            e.Type == "reserved" && e.StripeSessionId == stripeSessionId);
        if (reserved != null)
        {
            ledger.Entries.Remove(reserved);
            await WriteLedgerAsync(userId, ledger);
        }
    }
}
