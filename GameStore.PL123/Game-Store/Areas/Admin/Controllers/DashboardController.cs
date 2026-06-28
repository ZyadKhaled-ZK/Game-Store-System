using System.Text.Json;
using GameStore.BLL.Models;
using GameStore.PL.Models.Admin;
using Microsoft.EntityFrameworkCore;

namespace GameStore.PL.Areas.Admin.Controllers;

[Area("Admin")]
[ServiceFilter(typeof(AdminOnlyFilter))]
public class DashboardController : Controller
{
    private readonly IUserService _userService;
    private readonly IGameService _gameService;
    private readonly IOrderService _orderService;
    private readonly IOrderAnalyticsService _orderAnalytics;

    public DashboardController(IUserService userService, IGameService gameService,
        IOrderService orderService, IOrderAnalyticsService orderAnalytics)
    {
        _userService = userService;
        _gameService = gameService;
        _orderService = orderService;
        _orderAnalytics = orderAnalytics;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var model = new DashboardViewModel
        {
            TotalUsers = await _userService.GetTotalUsersAsync(),
            TotalGames = await _gameService.GetTotalGamesAsync(),
            FreeGamesCount = await _gameService.GetFreeGamesCountAsync(),
            TotalOrders = await _orderAnalytics.GetTotalOrdersAsync(),
            TotalRevenue = await _orderAnalytics.GetTotalRevenueAsync(),
            AverageOrderValue = await _orderAnalytics.GetAverageOrderValueAsync()
        };

        var revenueByMonth = await _orderAnalytics.GetRevenueByMonthAsync(12);
        model.RevenueByMonthJson = JsonSerializer.Serialize(revenueByMonth, DashboardViewModel.JsonOpts);

        var ordersByDay = await _orderAnalytics.GetOrdersByDayAsync(30);
        model.OrdersByDayJson = JsonSerializer.Serialize(ordersByDay, DashboardViewModel.JsonOpts);

        var topGames = await _orderAnalytics.GetTopSellingGamesAsync(5);
        model.TopSellingGamesJson = JsonSerializer.Serialize(topGames, DashboardViewModel.JsonOpts);

        var revenueByCat = await _orderAnalytics.GetRevenueByCategoryAsync();
        model.RevenueByCategoryJson = JsonSerializer.Serialize(revenueByCat, DashboardViewModel.JsonOpts);

        var usersByRole = await _userService.GetUsersByRoleAsync();
        model.UsersByRoleJson = JsonSerializer.Serialize(usersByRole, DashboardViewModel.JsonOpts);

        var gamesByCat = await _gameService.GetGamesByCategoryAsync();
        model.GamesByCategoryJson = JsonSerializer.Serialize(gamesByCat, DashboardViewModel.JsonOpts);

        var usersByMonth = await _userService.GetUsersByMonthAsync(12);
        model.UsersByMonthJson = JsonSerializer.Serialize(usersByMonth, DashboardViewModel.JsonOpts);

        // Use the analytics service for today's stats instead of loading all orders
        var today = DateTime.UtcNow.Date;
        model.TodayOrdersCount = await _orderAnalytics.GetOrderCountSinceAsync(today);
        model.TodayRevenue = await _orderAnalytics.GetRevenueSinceAsync(today);

        // Get only 5 recent orders with details
        var recentOrders = await _orderService.GetRecentWithDetailsAsync(5);
        model.RecentOrdersJson = JsonSerializer.Serialize(recentOrders.Select(o => new
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
        }), DashboardViewModel.JsonOpts);

        var lastMonth = usersByMonth.LastOrDefault();
        model.NewUsersThisMonth = lastMonth?.Count ?? 0;

        return View(model);
    }
}
