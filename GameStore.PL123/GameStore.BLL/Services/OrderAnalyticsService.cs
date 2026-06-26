using GameStore.BLL.Models;

namespace GameStore.BLL.Services
{
    public class OrderAnalyticsService : IOrderAnalyticsService
    {
        private readonly IUnitOfWork _uow;

        public OrderAnalyticsService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<int> GetTotalOrdersAsync()
        {
            return await _uow.Repository<Order>().CountAsync();
        }

        public async Task<decimal> GetTotalRevenueAsync()
        {
            return await _uow.Repository<Order>().Query()
                .SumAsync(o => o.TotalPrice);
        }

        public async Task<decimal> GetAverageOrderValueAsync()
        {
            if (!await _uow.Repository<Order>().AnyAsync()) return 0;
            return await _uow.Repository<Order>().Query().AverageAsync(o => o.TotalPrice);
        }

        public async Task<List<MonthlyRevenue>> GetRevenueByMonthAsync(int months = 12)
        {
            var since = DateTime.UtcNow.AddMonths(-months);
            var data = await _uow.Repository<Order>().Query()
                .Where(o => o.CreatedAt >= since)
                .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
                .Select(g => new MonthlyRevenue
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Revenue = g.Sum(o => o.TotalPrice)
                })
                .OrderBy(r => r.Year).ThenBy(r => r.Month)
                .ToListAsync();
            return data;
        }

        public async Task<List<DailyOrderCount>> GetOrdersByDayAsync(int days = 30)
        {
            var since = DateTime.UtcNow.AddDays(-days);
            var raw = await _uow.Repository<Order>().Query()
                .Where(o => o.CreatedAt >= since)
                .GroupBy(o => o.CreatedAt.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .OrderBy(d => d.Date)
                .ToListAsync();

            return raw.Select(d => new DailyOrderCount
            {
                Date = d.Date.ToString("yyyy-MM-dd"),
                Count = d.Count
            }).ToList();
        }

        public async Task<List<TopGameSale>> GetTopSellingGamesAsync(int count = 5)
        {
            var data = await _uow.Repository<OrderItem>().Query()
                .GroupBy(oi => oi.Game!.Title)
                .Select(g => new TopGameSale
                {
                    Title = g.Key,
                    Count = g.Count(),
                    Revenue = g.Sum(oi => oi.PriceAtPurchase)
                })
                .OrderByDescending(g => g.Count)
                .Take(count)
                .ToListAsync();
            return data;
        }

        public async Task<List<CategoryRevenue>> GetRevenueByCategoryAsync()
        {
            var query = from oi in _uow.Repository<OrderItem>().Query()
                        join g in _uow.Repository<Game>().Query() on oi.GameId equals g.Id
                        join gc in _uow.Repository<GameCategory>().Query() on g.Id equals gc.GameId
                        join c in _uow.Repository<Category>().Query() on gc.CategoryId equals c.Id
                        group oi by c.Name into g
                        select new CategoryRevenue
                        {
                            Category = g.Key,
                            Revenue = g.Sum(oi => oi.PriceAtPurchase)
                        };

            return await query.OrderByDescending(c => c.Revenue).ToListAsync();
        }

        public async Task<List<MonthlyRevenue>> GetOrdersCountByMonthAsync(int months = 12)
        {
            var since = DateTime.UtcNow.AddMonths(-months);
            var data = await _uow.Repository<Order>().Query()
                .Where(o => o.CreatedAt >= since)
                .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
                .Select(g => new MonthlyRevenue
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Revenue = g.Count()
                })
                .OrderBy(r => r.Year).ThenBy(r => r.Month)
                .ToListAsync();
            return data;
        }
    }
}
