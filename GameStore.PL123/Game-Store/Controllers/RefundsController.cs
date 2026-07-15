using GameStore.PL.Models.Refunds;

namespace GameStore.PL.Controllers;

public class RefundsController : Controller
{
    private readonly IRefundService _refundService;
    private readonly IOrderService _orderService;

    public RefundsController(IRefundService refundService, IOrderService orderService)
    {
        _refundService = refundService;
        _orderService = orderService;
    }

    private string UserId => HttpContext.Session.GetString("UserId") ?? string.Empty;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (string.IsNullOrEmpty(UserId))
            return RedirectToAction("Login", "Auth");

        // Admins use /Admin/Refunds
        if (HttpContext.Session.GetString("Role") == "ADMIN")
            return RedirectToAction("Index", "Refunds", new { area = "Admin" });

        var orders = await _orderService.GetOrdersByUserAsync(UserId);
        var requests = await _refundService.GetByUserAsync(UserId);

        var model = new RefundViewModel
        {
            Orders = orders.Where(o => o.PaymentStatus == PaymentStatus.Completed).ToList(),
            Requests = requests
        };

        if (TempData.TryGetValue("Message", out var msg)) model.Message = msg?.ToString() ?? "";
        if (TempData.TryGetValue("IsError", out var err)) model.IsError = err is bool b && b;

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(RefundRequestInput input)
    {
        if (string.IsNullOrEmpty(UserId))
            return RedirectToAction("Login", "Auth");

        if (HttpContext.Session.GetString("Role") == "ADMIN")
            return RedirectToAction("Index", "Refunds", new { area = "Admin" });

        var (success, message) = await _refundService.RequestAsync(input.OrderId, input.GameId, input.Reason, UserId);
        TempData["Message"] = message;
        TempData["IsError"] = !success;
        return RedirectToAction("Index");
    }
}
