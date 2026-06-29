using GameStore.PL.Filters;
using GameStore.PL.Services;

namespace GameStore.PL.Areas.Admin.Controllers;

[Area("Admin")]
[ServiceFilter(typeof(AdminOnlyFilter))]
public class SalesController : Controller
{
    private readonly ISaleService _saleService;
    private readonly IDeveloperService _devService;
    private readonly INotificationService _notifService;

    public SalesController(ISaleService saleService, IDeveloperService devService,
        INotificationService notifService)
    {
        _saleService = saleService;
        _devService = devService;
        _notifService = notifService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var sales = await _saleService.GetPendingAsync();

        if (TempData.TryGetValue("Message", out var msg)) ViewData["Message"] = msg;
        if (TempData.TryGetValue("IsError", out var err)) ViewData["IsError"] = err is bool b && b;

        return View(sales);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(string id)
    {
        if (string.IsNullOrEmpty(id)) return RedirectToAction("Index");

        var sale = await _saleService.GetByIdAsync(id);
        if (sale == null)
        {
            TempData["Message"] = "Sale not found.";
            TempData["IsError"] = true;
            return RedirectToAction("Index");
        }

        var (success, error) = await _saleService.ApproveAsync(id);
        TempData["Message"] = success ? "Sale approved." : error;
        TempData["IsError"] = !success;

        if (success && sale.Developer != null)
        {
            var gameTitle = sale.Game?.Title ?? "Unknown";
            await _notifService.SendToUserAsync(sale.Developer.UserId,
                "Sale Approved",
                $"Your sale for \"{gameTitle}\" has been approved. It will go live on {sale.StartDate:MMM dd, yyyy}.",
                "success");
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(string id, string? reason)
    {
        if (string.IsNullOrEmpty(id)) return RedirectToAction("Index");

        var sale = await _saleService.GetByIdAsync(id);
        var gameTitle = sale?.Game?.Title ?? "Unknown";

        var (success, error) = await _saleService.RejectAsync(id, reason ?? "");
        TempData["Message"] = success ? "Sale rejected." : error;
        TempData["IsError"] = !success;

        if (success && sale?.Developer != null)
        {
            var msg = string.IsNullOrEmpty(reason)
                ? $"Your sale for \"{gameTitle}\" has been rejected."
                : $"Your sale for \"{gameTitle}\" has been rejected. Reason: {reason}";
            await _notifService.SendToUserAsync(sale.Developer.UserId,
                "Sale Rejected", msg, "error");
        }

        return RedirectToAction("Index");
    }
}
