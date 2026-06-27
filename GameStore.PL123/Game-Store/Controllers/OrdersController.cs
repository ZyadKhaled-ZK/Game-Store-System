using GameStore.PL.Models.Orders;

namespace GameStore.PL.Controllers;

public class OrdersController : Controller
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Auth");

        var model = new OrderListViewModel
        {
            Orders = await _orderService.GetOrdersByUserAsync(userId)
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(string id)
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Auth");

        if (string.IsNullOrEmpty(id))
            return RedirectToAction("Index");

        var order = await _orderService.GetOrderByIdAsync(id);
        if (order == null || order.UserId != userId)
        {
            var model = new OrderDetailViewModel { Message = "Order not found.", IsError = true };
            return View(model);
        }

        return View(new OrderDetailViewModel { Order = order });
    }
}
