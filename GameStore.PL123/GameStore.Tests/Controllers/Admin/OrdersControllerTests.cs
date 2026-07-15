using FluentAssertions;
using GameStore.PL.Areas.Admin.Controllers;
using GameStore.PL.Models.Admin;
using Moq;

namespace GameStore.Tests.Controllers.Admin;

public class OrdersControllerTests
{
    [Fact]
    public async Task Index_Returns_View_With_Orders()
    {
        var orderSvc = new Mock<IOrderService>();
        var analyticsSvc = new Mock<IOrderAnalyticsService>();

        orderSvc.Setup(s => s.GetAllWithDetailsAsync()).ReturnsAsync([
            new Order { Id = "o1", UserId = "u1", TotalPrice = 10m },
            new Order { Id = "o2", UserId = "u1", TotalPrice = 20m }
        ]);
        analyticsSvc.Setup(s => s.GetTotalRevenueAsync()).ReturnsAsync(30m);

        var controller = new OrdersController(orderSvc.Object, analyticsSvc.Object);
        TestHelper.SetupController(controller);

        var result = await controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<AdminOrdersViewModel>(view.Model);
        model.Orders.Should().HaveCount(2);
        model.TotalOrders.Should().Be(2);
        model.TotalRevenue.Should().Be(30m);
    }
}
