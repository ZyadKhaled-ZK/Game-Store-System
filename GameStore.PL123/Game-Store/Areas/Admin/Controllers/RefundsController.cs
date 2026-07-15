namespace GameStore.PL.Areas.Admin.Controllers;

[Area("Admin")]
[ServiceFilter(typeof(AdminOnlyFilter))]
public class RefundsController : Controller
{
    private readonly IRefundService _refundService;
    private readonly ICreditService _creditService;

    public RefundsController(IRefundService refundService, ICreditService creditService)
    {
        _refundService = refundService;
        _creditService = creditService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var requests = await _refundService.GetAllAsync();
        var balances = new Dictionary<string, decimal>();
        foreach (var uid in requests.Select(r => r.UserId).Distinct())
            balances[uid] = await _creditService.GetBalanceAsync(uid);
        ViewBag.UserCredits = balances;
        return View(requests);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(string id, string note)
    {
        var (success, message) = await _refundService.ApproveAsync(id, note ?? "");
        TempData["Message"] = message;
        TempData["IsError"] = !success;
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(string id, string note)
    {
        var (success, message) = await _refundService.RejectAsync(id, note ?? "");
        TempData["Message"] = message;
        TempData["IsError"] = !success;
        return RedirectToAction("Index");
    }
}
