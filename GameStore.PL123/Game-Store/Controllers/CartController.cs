using GameStore.PL.Models.Cart;
using Microsoft.Extensions.Options;
using Stripe.Checkout;

namespace GameStore.PL.Controllers;

public class CartController : Controller
{
    private readonly ICartService _cartService;
    private readonly IOrderService _orderService;
    private readonly StripeSettings _stripeSettings;
    private readonly ISaleService _saleService;
    private readonly ICreditService _creditService;

    public CartController(ICartService cartService, IOrderService orderService,
        IOptions<StripeSettings> stripeOptions, ISaleService saleService,
        ICreditService creditService)
    {
        _cartService = cartService;
        _orderService = orderService;
        _stripeSettings = stripeOptions.Value;
        _saleService = saleService;
        _creditService = creditService;
    }

    private string UserId => HttpContext.Session.GetString("UserId") ?? string.Empty;

    private static string? ResolveImageUrl(string? imageUrl, string domain)
    {
        if (string.IsNullOrEmpty(imageUrl)) return null;
        return imageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? imageUrl : domain + imageUrl;
    }

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

        ViewData["StripePublishableKey"] = _stripeSettings.PublishableKey;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddToCart([FromBody] string gameId)
    {
        if (string.IsNullOrEmpty(UserId))
            return Json(new { success = false, message = "Please login first." });

        var (success, message) = await _cartService.AddToCartAsync(UserId, gameId);
        return Json(new { success, message });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(string id)
    {
        if (string.IsNullOrEmpty(UserId)) return Json(new { success = false, message = "Please login first." });

        await _cartService.RemoveFromCartAsync(id, UserId);
        TempData["Message"] = "Game removed from cart.";
        TempData["IsError"] = false;
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout()
    {
        if (string.IsNullOrEmpty(UserId)) return RedirectToAction("Login", "Auth");

        var cartItems = await _cartService.GetCartItemsAsync(UserId);
        if (!cartItems.Any())
        {
            TempData["Message"] = "Your cart is empty.";
            TempData["IsError"] = true;
            return RedirectToAction("Index");
        }

        if (cartItems.All(ci => ci.Game?.Price == 0))
        {
            var (success, message, order) = await _orderService.CompleteFreeCheckoutAsync(UserId);
            if (!success)
            {
                TempData["Message"] = message;
                TempData["IsError"] = true;
                return RedirectToAction("Index");
            }
            return View("Success", order);
        }

        var gameIds = cartItems.Select(ci => ci.GameId).ToList();
        var activeSales = await _saleService.GetActiveSalesByGameIdsAsync(gameIds);

        var totalPrice = cartItems.Sum(ci =>
        {
            var sale = activeSales.FirstOrDefault(s => s.GameId == ci.GameId);
            return sale != null ? sale.NewPrice : (ci.Game?.Price ?? 0);
        });
        var availableCredit = await _creditService.GetAvailableBalanceAsync(UserId);

        // Full-credit path — skip Stripe entirely
        if (availableCredit >= totalPrice)
        {
            var internalSessionId = "internal_" + Guid.NewGuid().ToString();
            var (resSuccess, resMsg) = await _creditService.ReserveAsync(UserId, totalPrice, internalSessionId);
            if (!resSuccess)
            {
                TempData["Message"] = resMsg;
                TempData["IsError"] = true;
                return RedirectToAction("Index");
            }

            var (success, message, order) = await _orderService.CompleteFreeCheckoutAsync(UserId);
            if (!success)
            {
                await _creditService.ReleaseReservationAsync(UserId, internalSessionId);
                TempData["Message"] = message;
                TempData["IsError"] = true;
                return RedirectToAction("Index");
            }

            await _creditService.ConfirmReservationAsync(UserId, internalSessionId,
                $"Store credit payment for order {order!.Id}");
            return View("Success", order);
        }

        var domain = $"{Request.Scheme}://{Request.Host}";

        // Partial credit: create a one-time Stripe coupon
        string? couponId = null;
        if (availableCredit > 0)
        {
            var creditCents = (long)(availableCredit * 100);
            var couponOpts = new Stripe.CouponCreateOptions
            {
                AmountOff = creditCents,
                Currency = "usd",
                Duration = "once",
                MaxRedemptions = 1,
                Name = "Store Credit"
            };
            var couponSvc = new Stripe.CouponService();
            var coupon = await couponSvc.CreateAsync(couponOpts);
            couponId = coupon.Id;
        }

        var options = new SessionCreateOptions
        {
            Mode = "payment",
            SuccessUrl = domain + Url.Action("Success", "Cart") + "?session_id={CHECKOUT_SESSION_ID}",
            CancelUrl = domain + Url.Action("Cancel", "Cart"),
            ClientReferenceId = UserId,
            Metadata = new Dictionary<string, string>
            {
                { "user_id", UserId }
            },
            Discounts = couponId != null
                ? new List<SessionDiscountOptions> { new() { Coupon = couponId } }
                : null,
            LineItems = cartItems.Select(ci =>
            {
                var sale = activeSales.FirstOrDefault(s => s.GameId == ci.GameId);
                var price = sale != null ? sale.NewPrice : (ci.Game?.Price ?? 0);
                var img = ResolveImageUrl(ci.Game?.CoverImageUrl, domain);
                return new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "usd",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = ci.Game?.Title ?? "Unknown Game",
                            Images = img != null ? new List<string> { img } : null
                        },
                        UnitAmount = (long)(price * 100)
                    },
                    Quantity = 1
                };
            }).ToList()
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options);

        // Reserve the credit against this Stripe session
        if (availableCredit > 0)
        {
            await _creditService.ReserveAsync(UserId, availableCredit, session.Id);
        }

        HttpContext.Session.SetString("StripeSessionId", session.Id);

        return Redirect(session.Url);
    }

    [HttpGet]
    public async Task<IActionResult> Success(string session_id)
    {
        if (string.IsNullOrEmpty(UserId)) return RedirectToAction("Login", "Auth");

        try
        {
            var service = new SessionService();
            var session = await service.GetAsync(session_id);

            if (session.PaymentStatus != "paid")
            {
                TempData["Message"] = "Payment not completed.";
                TempData["IsError"] = true;
                return RedirectToAction("Index");
            }

            var (success, message, order) = await _orderService.CompleteCheckoutAsync(
                UserId, session_id, session.PaymentIntentId);

            if (!success)
            {
                TempData["Message"] = message;
                TempData["IsError"] = true;
                return RedirectToAction("Index");
            }

            return View(order);
        }
        catch
        {
            TempData["Message"] = "Something went wrong verifying your payment.";
            TempData["IsError"] = true;
            return RedirectToAction("Index");
        }
    }

    [HttpGet]
    public IActionResult Cancel()
    {
        TempData["Message"] = "Payment cancelled.";
        TempData["IsError"] = true;
        return RedirectToAction("Index");
    }
}
