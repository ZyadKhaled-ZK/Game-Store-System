using GameStore.PL.Filters;

namespace GameStore.PL.Areas.Developer.Controllers;

[Area("Developer")]
[ServiceFilter(typeof(DeveloperOnlyFilter))]
public class DashboardController : Controller
{
    private readonly IDeveloperService _devService;
    private readonly IReviewService _reviewService;

    public DashboardController(IDeveloperService devService, IReviewService reviewService)
    {
        _devService = devService;
        _reviewService = reviewService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Auth");

        var dev = await _devService.GetByUserIdAsync(userId);

        ViewData["Title"] = "Dashboard";
        ViewData["HasProfile"] = dev != null;

        if (dev == null)
        {
            ViewData["GameCount"] = 0;
            ViewData["TotalDownloads"] = 0;
            ViewData["TotalReviews"] = 0;
            ViewData["TotalRevenue"] = 0;
            ViewData["AvgRating"] = 0.0;
            ViewData["GameStats"] = new List<(Game Game, int Downloads, double AvgRating, int ReviewCount)>();
            ViewData["RecentReviews"] = new List<Review>();
            return View();
        }

        var stats = await _devService.GetDashboardStatsAsync(dev.Id);
        ViewData["GameCount"] = stats.GameCount;
        ViewData["TotalDownloads"] = stats.TotalDownloads;
        ViewData["TotalReviews"] = stats.TotalReviews;
        ViewData["TotalRevenue"] = stats.TotalRevenue;
        ViewData["NetRevenue"] = stats.NetRevenue;
        ViewData["AvgRating"] = stats.AvgRating;
        ViewData["DeveloperName"] = dev.Name;

        var gameStats = await _devService.GetGameStatsAsync(dev.Id);
        ViewData["GameStats"] = gameStats;

        var gameIds = gameStats.Select(g => g.Game.Id).ToList();
        var allReviews = new List<Review>();
        foreach (var gid in gameIds)
        {
            var reviews = await _reviewService.GetByGameAsync(gid);
            allReviews.AddRange(reviews);
        }
        ViewData["RecentReviews"] = allReviews.OrderByDescending(r => r.CreatedAt).Take(5).ToList();

        return View(dev);
    }
}
