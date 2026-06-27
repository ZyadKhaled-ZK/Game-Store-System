using GameStore.PL.Models.Wishlist;

namespace GameStore.PL.Controllers;

public class WishlistController : Controller
{
    private readonly IWishlistService _wishlistService;
    private readonly ICartService _cartService;

    public WishlistController(IWishlistService wishlistService, ICartService cartService)
    {
        _wishlistService = wishlistService;
        _cartService = cartService;
    }

    private string UserId => HttpContext.Session.GetString("UserId") ?? string.Empty;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (string.IsNullOrEmpty(UserId))
            return RedirectToAction("Login", "Auth");

        var model = new WishlistViewModel
        {
            WishlistItems = await _wishlistService.GetWishlistAsync(UserId)
        };

        if (TempData.TryGetValue("Message", out var msg)) model.Message = msg?.ToString() ?? "";
        if (TempData.TryGetValue("IsError", out var err)) model.IsError = err is bool b && b;

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(string id)
    {
        if (string.IsNullOrEmpty(UserId)) return RedirectToAction("Login", "Auth");
        if (string.IsNullOrEmpty(id))
        {
            TempData["Message"] = "Invalid item.";
            TempData["IsError"] = true;
            return RedirectToAction("Index");
        }
        await _wishlistService.RemoveFromWishlistAsync(id);
        TempData["Message"] = "Removed from wishlist.";
        TempData["IsError"] = false;
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddToCart(string gameId)
    {
        if (string.IsNullOrEmpty(UserId)) return RedirectToAction("Login", "Auth");
        var (success, message) = await _cartService.AddToCartAsync(UserId, gameId);
        TempData["Message"] = success ? "Added to cart!" : message;
        TempData["IsError"] = !success;
        return RedirectToAction("Index");
    }
}
