using GameStore.PL.Models.Cart;

namespace GameStore.PL.Controllers;

public class CartController : Controller
{
    private readonly ICartService _cartService;
    private readonly IOrderService _orderService;

    public CartController(ICartService cartService, IOrderService orderService)
    {
        _cartService = cartService;
        _orderService = orderService;
    }

    private string UserId => HttpContext.Session.GetString("UserId") ?? string.Empty;

    [HttpGet]
    public async Task<IActionResult> GetCount()
    {
        if (string.IsNullOrEmpty(UserId))
            return Json(new { count = 0 });
        var items = await _cartService.GetCartItemsAsync(UserId);
        return Json(new { count = items.Count });
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var model = new CartViewModel();
        if (!string.IsNullOrEmpty(UserId))
            model.CartItems = await _cartService.GetCartItemsAsync(UserId);

        if (TempData.TryGetValue("Message", out var msg)) model.Message = msg?.ToString() ?? "";
        if (TempData.TryGetValue("IsError", out var err)) model.IsError = err is bool b && b;

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(string id)
    {
        if (string.IsNullOrEmpty(UserId)) return Json(new { success = false, message = "Please login first." });

        await _cartService.RemoveFromCartAsync(id);
        TempData["Message"] = "Game removed from cart.";
        TempData["IsError"] = false;
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout()
    {
        if (string.IsNullOrEmpty(UserId)) return RedirectToAction("Login", "Auth");

        var (success, message) = await _orderService.PlaceOrderAsync(UserId);
        TempData["Message"] = success ? "Order placed successfully!" : message;
        TempData["IsError"] = !success;
        return RedirectToAction("Index");
    }
}
