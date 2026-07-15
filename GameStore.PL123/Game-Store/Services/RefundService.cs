namespace GameStore.PL.Services;

public class RefundService : IRefundService
{
    private readonly IJsonFileStore _fileStore;
    private readonly IOrderService _orderService;
    private readonly ILibraryService _libraryService;
    private readonly INotificationService _notificationService;
    private readonly ICreditService _creditService;
    private const string StorePath = "refunds/_all.json";

    public RefundService(IJsonFileStore fileStore, IOrderService orderService,
        ILibraryService libraryService, INotificationService notificationService,
        ICreditService creditService)
    {
        _fileStore = fileStore;
        _orderService = orderService;
        _libraryService = libraryService;
        _notificationService = notificationService;
        _creditService = creditService;
    }

    public async Task<List<RefundRequest>> GetAllAsync()
    {
        return await _fileStore.ReadAsync<List<RefundRequest>>(StorePath)
            ?? new List<RefundRequest>();
    }

    public async Task<List<RefundRequest>> GetByUserAsync(string userId)
    {
        var all = await GetAllAsync();
        return all.Where(r => r.UserId == userId)
            .OrderByDescending(r => r.RequestedAt).ToList();
    }

    public async Task<RefundRequest?> GetByIdAsync(string refundId)
    {
        var all = await GetAllAsync();
        return all.FirstOrDefault(r => r.Id == refundId);
    }

    public async Task<(bool Success, string Message)> RequestAsync(string orderId, string gameId, string reason, string userId)
    {
        var order = await _orderService.GetOrderByIdAsync(orderId);
        if (order == null) return (false, "Order not found.");
        if (order.UserId != userId) return (false, "Order does not belong to you.");
        if (order.PaymentStatus != PaymentStatus.Completed) return (false, "Order cannot be refunded.");
        if (order.CreatedAt < DateTime.UtcNow.AddDays(-14)) return (false, "Refund window of 14 days has expired.");

        var game = order.OrderItems.FirstOrDefault(oi => oi.GameId == gameId)?.Game;
        if (game == null) return (false, "Game not found in order.");
        var amount = order.OrderItems.Where(oi => oi.GameId == gameId).Sum(oi => oi.PriceAtPurchase);
        if (amount <= 0) return (false, "Game is free; no refund needed.");

        var request = new RefundRequest
        {
            OrderId = orderId,
            UserId = userId,
            GameId = gameId,
            GameTitle = game.Title ?? "Unknown",
            Amount = amount,
            Reason = reason,
            Status = RefundStatus.Pending
        };

        var all = await GetAllAsync();
        all.Add(request);
        await _fileStore.WriteAsync(StorePath, all);

        await _notificationService.SendToAdminsAsync("New Refund Request",
            $"Refund request from {order.User?.Username ?? userId} for {game.Title} (${amount}).",
            "refund", referenceId: request.Id, referenceUrl: "/Admin/Refunds");

        return (true, "Refund requested.");
    }

    public async Task<(bool Success, string Message)> ApproveAsync(string refundId, string adminNote)
    {
        var all = await GetAllAsync();
        var request = all.FirstOrDefault(r => r.Id == refundId);
        if (request == null) return (false, "Refund request not found.");
        if (request.Status != RefundStatus.Pending) return (false, "Refund is not in pending state.");

        // Stage 1: mark as PendingFinalize to prevent double-processing
        request.Status = RefundStatus.PendingFinalize;
        request.AdminNote = adminNote;
        await _fileStore.WriteAsync(StorePath, all);

        try
        {
            // Stage 2: add store credit instead of Stripe refund
            await _creditService.AddCreditAsync(request.UserId, request.Amount,
                $"Refund for {request.GameTitle} (order {request.OrderId})");

            // Stage 3: update DB — remove game from library, mark order refunded
            await _orderService.SetPaymentStatusAsync(request.OrderId, PaymentStatus.Refunded);
            await _libraryService.RemoveGameFromLibraryAsync(request.UserId, request.GameId);

            // Stage 4: finalize JSON
            request.Status = RefundStatus.Approved;
            request.ResolvedAt = DateTime.UtcNow;
            request.StripeRefundId = "store_credit";
            await _fileStore.WriteAsync(StorePath, all);

            await _notificationService.SendToUserAsync(request.UserId,
                "Refund Approved — Store Credit Added",
                $"${request.Amount:F2} store credit added for {request.GameTitle}. It will auto-apply on your next purchase.",
                "refund", referenceId: refundId);

            return (true, $"Refund approved. ${request.Amount:F2} store credit added.");
        }
        catch (Exception ex)
        {
            request.Status = RefundStatus.Pending;
            request.AdminNote = $"Processing failed: {ex.Message}";
            await _fileStore.WriteAsync(StorePath, all);

            await _notificationService.SendToAdminsAsync("Refund Processing Failed",
                $"Refund {refundId} for {request.GameTitle} failed: {ex.Message}",
                "refund", referenceId: refundId);

            return (false, $"Failed: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> RejectAsync(string refundId, string adminNote)
    {
        var all = await GetAllAsync();
        var request = all.FirstOrDefault(r => r.Id == refundId);
        if (request == null) return (false, "Refund request not found.");
        if (request.Status != RefundStatus.Pending) return (false, "Refund is not in pending state.");

        request.Status = RefundStatus.Rejected;
        request.ResolvedAt = DateTime.UtcNow;
        request.AdminNote = adminNote;
        await _fileStore.WriteAsync(StorePath, all);

        await _notificationService.SendToUserAsync(request.UserId,
            "Refund Rejected",
            $"Your refund for {request.GameTitle} was rejected. Reason: {adminNote}",
            "refund", referenceId: refundId);

        return (true, "Refund rejected.");
    }
}
