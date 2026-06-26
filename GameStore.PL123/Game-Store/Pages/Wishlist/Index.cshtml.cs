namespace GameStore.PL.Pages.Wishlist
{
    public class IndexModel : PageModel
    {
        private readonly IWishlistService _wishlistService;
        private readonly ICartService _cartService;

        public IndexModel(IWishlistService wishlistService, ICartService cartService)
        {
            _wishlistService = wishlistService;
            _cartService = cartService;
        }

        public List<WishlistItem> WishlistItems { get; set; } = new();
        public string Message { get; set; } = string.Empty;
        public bool IsError { get; set; }

        private string UserId => HttpContext.Session.GetString("UserId") ?? string.Empty;

        public async Task<IActionResult> OnGet()
        {
            if (string.IsNullOrEmpty(UserId))
                return RedirectToPage("/Auth/Login");

            WishlistItems = await _wishlistService.GetWishlistAsync(UserId);
            if (TempData.TryGetValue("Message", out var msg)) Message = msg?.ToString() ?? "";
            if (TempData.TryGetValue("IsError", out var err)) IsError = err is bool b && b;
            return Page();
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostRemoveAsync(string id)
        {
            if (string.IsNullOrEmpty(UserId)) return RedirectToPage("/Auth/Login");
            await _wishlistService.RemoveFromWishlistAsync(id);
            TempData["Message"] = "Removed from wishlist.";
            TempData["IsError"] = false;
            return RedirectToPage();
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAddToCartAsync(string gameId)
        {
            if (string.IsNullOrEmpty(UserId)) return RedirectToPage("/Auth/Login");
            var (success, message) = await _cartService.AddToCartAsync(UserId, gameId);
            TempData["Message"] = success ? "Added to cart!" : message;
            TempData["IsError"] = !success;
            return RedirectToPage();
        }
    }
}
