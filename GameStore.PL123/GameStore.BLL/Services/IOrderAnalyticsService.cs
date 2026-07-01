using GameStore.BLL.Models;

namespace GameStore.BLL.Services
{
    public interface IOrderAnalyticsService
    {
        Task<int> GetTotalOrdersAsync();
        Task<decimal> GetTotalRevenueAsync();
        Task<decimal> GetAverageOrderValueAsync();
        Task<List<MonthlyRevenue>> GetRevenueByMonthAsync(int months = 12);
        Task<List<DailyOrderCount>> GetOrdersByDayAsync(int days = 30);
        Task<List<TopGameSale>> GetTopSellingGamesAsync(int count = 5);
        Task<List<CategoryRevenue>> GetRevenueByCategoryAsync();
        Task<int> GetOrderCountSinceAsync(DateTime since);
        Task<decimal> GetRevenueSinceAsync(DateTime since);
    }
}
