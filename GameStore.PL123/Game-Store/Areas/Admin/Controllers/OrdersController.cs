using GameStore.PL.Models.Admin;

namespace GameStore.PL.Areas.Admin.Controllers;

[Area("Admin")]
[ServiceFilter(typeof(AdminOnlyFilter))]
public class OrdersController : Controller
{
    private readonly IOrderService _orderService;
    private readonly IOrderAnalyticsService _orderAnalytics;

    public OrdersController(IOrderService orderService, IOrderAnalyticsService orderAnalytics)
    {
        _orderService = orderService;
        _orderAnalytics = orderAnalytics;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var model = new AdminOrdersViewModel
        {
            Orders = await _orderService.GetAllWithDetailsAsync(),
            TotalOrders = (await _orderService.GetAllWithDetailsAsync()).Count,
            TotalRevenue = await _orderAnalytics.GetTotalRevenueAsync()
        };

        return View(model);
    }
}
