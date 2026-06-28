using GameStore.PL.Filters;

namespace GameStore.PL.Areas.Developer.Controllers;

[Area("Developer")]
[ServiceFilter(typeof(DeveloperOnlyFilter))]
public class ReviewsController : Controller
{
    private readonly IDeveloperService _devService;
    private readonly IReviewService _reviewService;

    public ReviewsController(IDeveloperService devService, IReviewService reviewService)
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
        if (dev == null)
        {
            TempData["Message"] = "Create your developer profile first.";
            TempData["IsError"] = true;
            return RedirectToAction("Index", "Profile");
        }

        ViewData["Title"] = "Game Reviews";

        var games = await _devService.GetGamesAsync(dev.Id);
        var gameIds = games.Select(g => g.Id).ToList();

        var allReviews = new List<Review>();
        foreach (var gid in gameIds)
        {
            var reviews = await _reviewService.GetByGameAsync(gid);
            allReviews.AddRange(reviews);
        }

        allReviews = allReviews.OrderByDescending(r => r.CreatedAt).ToList();

        var gameLookup = games.ToDictionary(g => g.Id, g => g.Title);

        ViewData["GameLookup"] = gameLookup;

        return View(allReviews);
    }
}
