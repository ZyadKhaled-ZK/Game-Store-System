namespace GameStore.PL.Pages.Orders
{
    public class DetailsModel : PageModel
    {
        private readonly IOrderService _orderService;

        public DetailsModel(IOrderService orderService)
        {
            _orderService = orderService;
        }

        public Order Order { get; set; } = null!;
        public string Message { get; set; } = string.Empty;
        public bool IsError { get; set; }

        public async Task<IActionResult> OnGet(string id)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
                return RedirectToPage("/Auth/Login");

            if (string.IsNullOrEmpty(id))
                return RedirectToPage("/Orders/Index");

            var order = await _orderService.GetOrderByIdAsync(id);
            if (order == null || order.UserId != userId)
            {
                Message = "Order not found.";
                IsError = true;
                return Page();
            }

            Order = order;
            return Page();
        }
    }
}
