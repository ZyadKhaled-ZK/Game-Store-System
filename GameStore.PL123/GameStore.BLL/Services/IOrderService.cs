namespace GameStore.BLL.Services
{
    public interface IOrderService
    {
        Task<List<Order>> GetAllWithDetailsAsync();
        Task<bool> UpdateStatusAsync(string orderId, OrderStatus status);
        Task<int> GetTotalOrdersAsync();
        Task<int> GetCompletedOrdersAsync();
        Task<decimal> GetTotalRevenueAsync();
    }
}
