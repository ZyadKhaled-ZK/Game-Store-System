namespace GameStore.PL.Pages.Admin
{
    public class OrdersModel : PageModel
    {
        private readonly IOrderService _orderService;
        private readonly IOrderAnalyticsService _orderAnalytics;

        public OrdersModel(IOrderService orderService, IOrderAnalyticsService orderAnalytics)
        {
            _orderService = orderService;
            _orderAnalytics = orderAnalytics;
        }

        public List<Order> Orders { get; set; } = new();
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }

        public async Task<IActionResult> OnGet()
        {
            Orders = await _orderService.GetAllWithDetailsAsync();
            TotalOrders = Orders.Count;
            TotalRevenue = await _orderAnalytics.GetTotalRevenueAsync();

            return Page();
        }
    }
}
