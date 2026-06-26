using System.ComponentModel.DataAnnotations;

namespace GameStore.PL.Pages.Cart
{
    public class IndexModel : PageModel
    {
        private readonly ICartService _cartService;
        private readonly IOrderService _orderService;

        public IndexModel(ICartService cartService, IOrderService orderService)
        {
            _cartService = cartService;
            _orderService = orderService;
        }

        public List<CartItem> CartItems { get; set; } = new();
        public decimal TotalPrice => CartItems.Sum(ci => ci.Game?.Price ?? 0);
        public string Message { get; set; } = string.Empty;
        public bool IsError { get; set; }

        private string UserId => HttpContext.Session.GetString("UserId") ?? string.Empty;

        public async Task<IActionResult> OnGet()
        {
            if (!string.IsNullOrEmpty(UserId))
                CartItems = await _cartService.GetCartItemsAsync(UserId);
            if (TempData.TryGetValue("Message", out var msg)) Message = msg?.ToString() ?? "";
            if (TempData.TryGetValue("IsError", out var err)) IsError = err is bool b && b;
            return Page();
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostRemoveAsync([Required] string id)
        {
            if (!ModelState.IsValid) return RedirectToPage();
            if (string.IsNullOrEmpty(UserId)) return new JsonResult(new { success = false, message = "Please login first." });
            await _cartService.RemoveFromCartAsync(id);
            TempData["Message"] = "Game removed from cart.";
            TempData["IsError"] = false;
            return RedirectToPage();
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostCheckoutAsync()
        {
            if (string.IsNullOrEmpty(UserId)) return RedirectToPage("/Auth/Login");

            var (success, message) = await _orderService.PlaceOrderAsync(UserId);
            if (!success)
            {
                CartItems = await _cartService.GetCartItemsAsync(UserId);
                Message = message;
                IsError = true;
                return Page();
            }

            Message = "Order placed successfully!";
            IsError = false;
            CartItems = new();
            return Page();
        }
    }
}
