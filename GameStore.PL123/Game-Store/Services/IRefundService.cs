namespace GameStore.PL.Services;

public enum RefundStatus
{
    Pending,
    PendingFinalize,
    Approved,
    Rejected
}

public class RefundRequest
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string OrderId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string GameId { get; set; } = string.Empty;
    public string GameTitle { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public RefundStatus Status { get; set; } = RefundStatus.Pending;
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
    public string? AdminNote { get; set; }
    public string? StripeRefundId { get; set; }
}

public interface IRefundService
{
    Task<List<RefundRequest>> GetAllAsync();
    Task<List<RefundRequest>> GetByUserAsync(string userId);
    Task<RefundRequest?> GetByIdAsync(string refundId);
    Task<(bool Success, string Message)> RequestAsync(string orderId, string gameId, string reason, string userId);
    Task<(bool Success, string Message)> ApproveAsync(string refundId, string adminNote);
    Task<(bool Success, string Message)> RejectAsync(string refundId, string adminNote);
}
