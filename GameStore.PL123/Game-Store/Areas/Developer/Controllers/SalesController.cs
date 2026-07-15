using GameStore.PL.Filters;

namespace GameStore.PL.Areas.Developer.Controllers;

[Area("Developer")]
[ServiceFilter(typeof(DeveloperOnlyFilter))]
public class SalesController : Controller
{
    private readonly IDeveloperService _devService;
    private readonly ISaleService _saleService;
    private readonly IGameService _gameService;
    private readonly INotificationService _notificationService;

    public SalesController(IDeveloperService devService, ISaleService saleService, IGameService gameService, INotificationService notificationService)
    {
        _devService = devService;
        _saleService = saleService;
        _gameService = gameService;
        _notificationService = notificationService;
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

        var result = await _saleService.GetByDeveloperAsync(dev.Id, page);
        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
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

        var games = await _gameService.GetAllWithCategoriesAsync();
        var devGameIds = games.Where(g => g.DeveloperId == dev.Id).Select(g => g.Id).ToHashSet();
        ViewData["Games"] = games.Where(g => devGameIds.Contains(g.Id)).ToList();

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string gameId, decimal newPrice, DateTime startDate, DateTime endDate)
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Auth");

        var dev = await _devService.GetByUserIdAsync(userId);
        if (dev == null) return RedirectToAction("Index", "Profile");

        var (success, error) = await _saleService.CreateAsync(dev.Id, gameId, newPrice, startDate, endDate);
        TempData["Message"] = success ? "Sale request submitted for approval." : error;
        TempData["IsError"] = !success;

        if (success)
        {
            var game = await _gameService.GetByIdAsync(gameId);
            await _notificationService.SendToAdminsAsync(
                "New Sale Request",
                $"{dev.Name} requested a sale on \"{game?.Title ?? gameId}\" — new price: ${newPrice:F2}",
                "warning",
                "NewSaleRequest",
                gameId,
                "/Admin/Sales");
        }

        return RedirectToAction("Index");
    }
}
