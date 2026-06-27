using System.Text.Json;
using GameStore.BLL.Models;

namespace GameStore.PL.Models.Admin;

public class DashboardViewModel
{
    public int TotalUsers { get; set; }
    public int TotalGames { get; set; }
    public int FreeGamesCount { get; set; }
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AverageOrderValue { get; set; }

    public string RevenueByMonthJson { get; set; } = "[]";
    public string OrdersByDayJson { get; set; } = "[]";
    public string TopSellingGamesJson { get; set; } = "[]";
    public string RevenueByCategoryJson { get; set; } = "[]";
    public string UsersByRoleJson { get; set; } = "[]";
    public string GamesByCategoryJson { get; set; } = "[]";
    public string UsersByMonthJson { get; set; } = "[]";
    public string RecentOrdersJson { get; set; } = "[]";

    public static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };
}
