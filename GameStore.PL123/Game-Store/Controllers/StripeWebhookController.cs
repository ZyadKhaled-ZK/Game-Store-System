using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace GameStore.PL.Controllers;

[Route("stripe/webhook")]
[ApiController]
public class StripeWebhookController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly StripeSettings _stripeSettings;

    public StripeWebhookController(IOrderService orderService,
        IOptions<StripeSettings> stripeOptions)
    {
        _orderService = orderService;
        _stripeSettings = stripeOptions.Value;
    }

    [HttpPost]
    public async Task<IActionResult> Index()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(
                json,
                Request.Headers["Stripe-Signature"],
                _stripeSettings.WebhookSecret);
        }
        catch
        {
            return BadRequest("Invalid signature.");
        }

        if (stripeEvent.Type == "checkout.session.completed")
        {
            var session = stripeEvent.Data.Object as Session;
            if (session == null) return Ok();

            var order = await _orderService.GetByStripeSessionIdAsync(session.Id);
            if (order != null) return Ok();

            var userId = session.Metadata?.ContainsKey("user_id") == true
                ? session.Metadata["user_id"]
                : null;

            if (string.IsNullOrEmpty(userId))
            {
                var clientRef = session.ClientReferenceId;
                if (!string.IsNullOrEmpty(clientRef))
                    userId = clientRef;
            }

            if (!string.IsNullOrEmpty(userId) && session.PaymentStatus == "paid")
            {
                await _orderService.CompleteCheckoutAsync(
                    userId, session.Id, session.PaymentIntentId);
            }
        }

        return Ok();
    }
}
