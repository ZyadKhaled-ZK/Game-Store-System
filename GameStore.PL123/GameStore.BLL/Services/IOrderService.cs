namespace GameStore.BLL.Services
{
    public interface IOrderService
    {
        Task<List<Order>> GetAllWithDetailsAsync();
        Task<(bool Success, string Message)> PlaceOrderAsync(string userId);
        Task<List<Order>> GetOrdersByUserAsync(string userId);
        Task<Order?> GetOrderByIdAsync(string orderId);
    }
}
