namespace GameStore.PL.Pages.Orders
{
    public class IndexModel : PageModel
    {
        private readonly IOrderService _orderService;

        public IndexModel(IOrderService orderService)
        {
            _orderService = orderService;
        }

        public List<Order> Orders { get; set; } = new();

        public async Task<IActionResult> OnGet()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
                return RedirectToPage("/Auth/Login");

            Orders = await _orderService.GetOrdersByUserAsync(userId);
            return Page();
        }
    }
}
