namespace GameStore.PL.Pages.Admin
{
    public class OrdersModel : PageModel
    {
        private readonly IOrderService _orderService;

        public OrdersModel(IOrderService orderService)
        {
            _orderService = orderService;
        }

        public List<Order> Orders { get; set; } = new();
        public int TotalOrders { get; set; }
        public int CompletedOrders { get; set; }
        public decimal TotalRevenue { get; set; }

        public async Task<IActionResult> OnGet()
        {
            Orders = await _orderService.GetAllWithDetailsAsync();
            TotalOrders = Orders.Count;
            CompletedOrders = Orders.Count(o => o.Status == OrderStatus.COMPLETED);
            TotalRevenue = Orders.Where(o => o.Status == OrderStatus.COMPLETED).Sum(o => o.TotalPrice);

            return Page();
        }

        public async Task<IActionResult> OnPostUpdateStatusAsync(string orderId, int status)
        {
            await _orderService.UpdateStatusAsync(orderId, (OrderStatus)status);
            return RedirectToPage();
        }
    }
}
