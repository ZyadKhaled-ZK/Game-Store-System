using GameStore.PL.Filters;
using GameStore.PL.Models.Admin;
using GameStore.PL.Services;

namespace GameStore.PL.Areas.Admin.Controllers;

[Area("Admin")]
[ServiceFilter(typeof(AdminOnlyFilter))]
public class DevelopersController : Controller
{
    private readonly IDeveloperService _devService;
    private readonly IUserService _userService;
    private readonly INotificationService _notifService;

    public DevelopersController(IDeveloperService devService, IUserService userService, INotificationService notifService)
    {
        _devService = devService;
        _userService = userService;
        _notifService = notifService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var developers = await _devService.GetAllAsync();
        var model = new ManageDevelopersViewModel { Developers = developers };

        if (TempData.TryGetValue("Message", out var msg)) model.Message = msg?.ToString() ?? "";
        if (TempData.TryGetValue("IsError", out var err)) model.IsError = err is bool b && b;

        var gameCounts = new Dictionary<string, int>();
        foreach (var dev in developers)
        {
            var games = await _devService.GetGamesAsync(dev.Id);
            gameCounts[dev.Id] = games.Count;
        }
        ViewData["GameCounts"] = gameCounts;

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(string id)
    {
        if (string.IsNullOrEmpty(id)) return RedirectToAction("Index");

        var dev = await _devService.GetByIdAsync(id);
        if (dev == null) return RedirectToAction("Index");

        var games = await _devService.GetGamesAsync(id);
        var stats = await _devService.GetDashboardStatsAsync(id);
        var gameStats = await _devService.GetGameStatsAsync(id);

        ViewData["Games"] = games;
        ViewData["Stats"] = stats;
        ViewData["GameStats"] = gameStats;

        return View(dev);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Demote(string id)
    {
        if (string.IsNullOrEmpty(id)) return RedirectToAction("Index");

        var dev = await _devService.GetByIdAsync(id);
        var devName = dev?.Name ?? "Developer";

        var currentUserId = HttpContext.Session.GetString("UserId");
        var (success, error) = await _devService.DemoteAsync(id, currentUserId);

        TempData["Message"] = success ? "Developer demoted to customer." : error;
        TempData["IsError"] = !success;

        if (success && dev != null)
        {
            await _notifService.SendToUserAsync(dev.UserId,
                "Developer Access Revoked",
                $"Your developer account for \"{devName}\" has been demoted. Your games are no longer visible in the store.",
                "warning");
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        if (string.IsNullOrEmpty(id)) return RedirectToAction("Index");

        var (success, error) = await _devService.DeleteAsync(id);
        TempData["Message"] = success ? "Developer profile removed." : error;
        TempData["IsError"] = !success;
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reactivate(string id)
    {
        if (string.IsNullOrEmpty(id)) return RedirectToAction("Index");

        var dev = await _devService.GetByIdAsync(id);
        var devName = dev?.Name ?? "Developer";

        var currentUserId = HttpContext.Session.GetString("UserId");
        var (success, error) = await _devService.ReactivateAsync(id, currentUserId);

        TempData["Message"] = success ? "Developer access restored." : error;
        TempData["IsError"] = !success;

        if (success && dev != null)
        {
            await _notifService.SendToUserAsync(dev.UserId,
                "Developer Access Restored",
                $"Your developer account for \"{devName}\" has been reactivated! Your games are back in the store.",
                "success");
        }

        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        if (string.IsNullOrEmpty(id)) return RedirectToAction("Index");

        var dev = await _devService.GetByIdAsync(id);
        if (dev == null) return RedirectToAction("Index");

        return View(dev);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, string name, string? description, string? website, string? country)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Message"] = "Studio name is required.";
            TempData["IsError"] = true;
            return RedirectToAction("Edit", new { id });
        }

        var dev = await _devService.GetByIdAsync(id);
        if (dev == null) return RedirectToAction("Index");

        dev.Name = name;
        dev.Description = description;
        dev.Website = website;
        dev.Country = country;

        var userId = dev.UserId;
        var (_, _) = await _devService.CreateOrUpdateProfileAsync(userId, name, null, description, website, null, country);

        TempData["Message"] = "Developer profile updated.";
        TempData["IsError"] = false;
        return RedirectToAction("Index");
    }
}
