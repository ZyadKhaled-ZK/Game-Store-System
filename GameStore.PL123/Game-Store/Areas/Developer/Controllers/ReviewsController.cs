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
    public async Task<IActionResult> Index(int page = 1)
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

        var result = await _reviewService.GetByDeveloperAsync(dev.Id, page);
        var games = await _devService.GetGamesAsync(dev.Id);
        var gameLookup = games.ToDictionary(g => g.Id, g => g.Title);

        ViewData["GameLookup"] = gameLookup;

        return View(result);
    }
}
