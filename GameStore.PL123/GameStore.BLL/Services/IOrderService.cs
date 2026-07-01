namespace GameStore.BLL.Services
{
    public interface IOrderService
    {
        Task<List<Order>> GetAllWithDetailsAsync();
    Task<List<Order>> GetOrdersByUserAsync(string userId);
    Task<Order?> GetOrderByIdAsync(string orderId);
    Task<(bool Success, string Message, Order? Order)> CompleteCheckoutAsync(string userId, string stripeSessionId, string stripePaymentIntentId);
    Task<(bool Success, string Message, Order? Order)> CompleteFreeCheckoutAsync(string userId);
    Task<Order?> GetByStripeSessionIdAsync(string sessionId);
        Task<List<Order>> GetRecentWithDetailsAsync(int count);
    }
}
