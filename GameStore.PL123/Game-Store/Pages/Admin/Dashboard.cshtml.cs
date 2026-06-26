using System.Text.Json;
using GameStore.BLL.Models;

namespace GameStore.PL.Pages.Admin
{
    public class DashboardModel : PageModel
    {
        private readonly IUserService _userService;
        private readonly IGameService _gameService;
        private readonly IOrderService _orderService;
        private readonly IOrderAnalyticsService _orderAnalytics;

        public DashboardModel(IUserService userService, IGameService gameService,
            IOrderService orderService, IOrderAnalyticsService orderAnalytics)
        {
            _userService = userService;
            _gameService = gameService;
            _orderService = orderService;
            _orderAnalytics = orderAnalytics;
        }

        // ── KPIs ──
        public int TotalUsers { get; set; }
        public int TotalGames { get; set; }
        public int FreeGamesCount { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageOrderValue { get; set; }

        // ── Chart Data (serialized JSON) ──
        public string RevenueByMonthJson { get; set; } = "[]";
        public string OrdersByDayJson { get; set; } = "[]";
        public string TopSellingGamesJson { get; set; } = "[]";
        public string RevenueByCategoryJson { get; set; } = "[]";
        public string UsersByRoleJson { get; set; } = "[]";
        public string GamesByCategoryJson { get; set; } = "[]";
        public string UsersByMonthJson { get; set; } = "[]";

        // ── Recent Orders ──
        public string RecentOrdersJson { get; set; } = "[]";

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        public async Task<IActionResult> OnGet()
        {
            TotalUsers = await _userService.GetTotalUsersAsync();
            TotalGames = await _gameService.GetTotalGamesAsync();
            FreeGamesCount = await _gameService.GetFreeGamesCountAsync();
            TotalOrders = await _orderAnalytics.GetTotalOrdersAsync();
            TotalRevenue = await _orderAnalytics.GetTotalRevenueAsync();
            AverageOrderValue = await _orderAnalytics.GetAverageOrderValueAsync();

            // Chart data
            var revenueByMonth = await _orderAnalytics.GetRevenueByMonthAsync(12);
            RevenueByMonthJson = JsonSerializer.Serialize(revenueByMonth, JsonOpts);

            var ordersByDay = await _orderAnalytics.GetOrdersByDayAsync(30);
            OrdersByDayJson = JsonSerializer.Serialize(ordersByDay, JsonOpts);

            var topGames = await _orderAnalytics.GetTopSellingGamesAsync(5);
            TopSellingGamesJson = JsonSerializer.Serialize(topGames, JsonOpts);

            var revenueByCat = await _orderAnalytics.GetRevenueByCategoryAsync();
            RevenueByCategoryJson = JsonSerializer.Serialize(revenueByCat, JsonOpts);

            var usersByRole = await _userService.GetUsersByRoleAsync();
            UsersByRoleJson = JsonSerializer.Serialize(usersByRole, JsonOpts);

            var gamesByCat = await _gameService.GetGamesByCategoryAsync();
            GamesByCategoryJson = JsonSerializer.Serialize(gamesByCat, JsonOpts);

            var usersByMonth = await _userService.GetUsersByMonthAsync(12);
            UsersByMonthJson = JsonSerializer.Serialize(usersByMonth, JsonOpts);

            // Recent 5 orders
            var recentOrders = await _orderService.GetAllWithDetailsAsync();
            RecentOrdersJson = JsonSerializer.Serialize(recentOrders.Take(5).Select(o => new
            {
                o.Id,
                user = o.User?.Username ?? "N/A",
                o.TotalPrice,
                createdAt = o.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                items = o.OrderItems.Select(oi => new
                {
                    game = oi.Game?.Title ?? "N/A",
                    oi.PriceAtPurchase
                }).ToList()
            }), JsonOpts);

            return Page();
        }
    }
}
