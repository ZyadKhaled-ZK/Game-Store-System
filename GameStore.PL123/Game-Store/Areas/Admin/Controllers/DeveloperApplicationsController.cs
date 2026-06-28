using GameStore.PL.Filters;
using GameStore.PL.Services;

namespace GameStore.PL.Areas.Admin.Controllers;

[Area("Admin")]
[ServiceFilter(typeof(AdminOnlyFilter))]
public class DeveloperApplicationsController : Controller
{
    private readonly IDeveloperApplicationService _appService;
    private readonly INotificationService _notifService;

    public DeveloperApplicationsController(IDeveloperApplicationService appService, INotificationService notifService)
    {
        _appService = appService;
        _notifService = notifService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var applications = await _appService.GetAllAsync();

        if (TempData.TryGetValue("Message", out var msg)) ViewData["Message"] = msg;
        if (TempData.TryGetValue("IsError", out var err)) ViewData["IsError"] = err is bool b && b;

        return View(applications);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(string id)
    {
        if (string.IsNullOrEmpty(id)) return RedirectToAction("Index");

        var app = await _appService.GetByIdAsync(id);
        if (app == null)
        {
            TempData["Message"] = "Application not found.";
            TempData["IsError"] = true;
            return RedirectToAction("Index");
        }

        var (success, error) = await _appService.ApproveAsync(id);
        TempData["Message"] = success ? "Developer application approved." : error;
        TempData["IsError"] = !success;

        if (success)
        {
            await _notifService.SendToUserAsync(app.UserId,
                "Application Approved",
                $"Your developer application for \"{app.Name}\" has been approved! Welcome to the Developer Portal.",
                "success");
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(string id)
    {
        if (string.IsNullOrEmpty(id)) return RedirectToAction("Index");

        var app = await _appService.GetByIdAsync(id);
        var appName = app?.Name ?? "your studio";

        var (success, error) = await _appService.RejectAsync(id);
        TempData["Message"] = success ? "Developer application rejected." : error;
        TempData["IsError"] = !success;

        if (success && app != null)
        {
            await _notifService.SendToUserAsync(app.UserId,
                "Application Rejected",
                $"Your developer application for \"{appName}\" has been rejected. You may re-apply at any time.",
                "error");
        }

        return RedirectToAction("Index");
    }
}
