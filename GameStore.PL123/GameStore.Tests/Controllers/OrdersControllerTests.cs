using FluentAssertions;
using GameStore.PL.Models.Orders;
using Moq;

namespace GameStore.Tests.Controllers;

public class OrdersControllerTests
{
    [Fact]
    public async Task Index_Returns_View_When_Logged_In()
    {
        var orderSvc = new Mock<IOrderService>();
        orderSvc.Setup(s => s.GetOrdersByUserAsync("u1")).ReturnsAsync(new List<Order>());

        var controller = new GameStore.PL.Controllers.OrdersController(orderSvc.Object);
        TestHelper.SetupController(controller, userId: "u1");

        var result = await controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<OrderListViewModel>(view.Model);
        model.Orders.Should().BeEmpty();
    }

    [Fact]
    public async Task Index_Redirects_When_Not_Logged_In()
    {
        var controller = new GameStore.PL.Controllers.OrdersController(Mock.Of<IOrderService>());
        TestHelper.SetupController(controller);

        var result = await controller.Index();

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        redirect.ActionName.Should().Be("Login");
    }

    [Fact]
    public async Task Details_Returns_View_For_Own_Order()
    {
        var orderSvc = new Mock<IOrderService>();
        orderSvc.Setup(s => s.GetOrderByIdAsync("o1")).ReturnsAsync(
            new Order { Id = "o1", UserId = "u1", TotalPrice = 10m });

        var controller = new GameStore.PL.Controllers.OrdersController(orderSvc.Object);
        TestHelper.SetupController(controller, userId: "u1");

        var result = await controller.Details("o1");

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<OrderDetailViewModel>(view.Model);
        model.Order.Should().NotBeNull();
    }

    [Fact]
    public async Task Details_Returns_Error_For_Other_Users_Order()
    {
        var orderSvc = new Mock<IOrderService>();
        orderSvc.Setup(s => s.GetOrderByIdAsync("o1")).ReturnsAsync(
            new Order { Id = "o1", UserId = "u2", TotalPrice = 10m });

        var controller = new GameStore.PL.Controllers.OrdersController(orderSvc.Object);
        TestHelper.SetupController(controller, userId: "u1");

        var result = await controller.Details("o1");

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<OrderDetailViewModel>(view.Model);
        model.IsError.Should().BeTrue();
    }

    [Fact]
    public async Task Details_Redirects_When_Not_Logged_In()
    {
        var controller = new GameStore.PL.Controllers.OrdersController(Mock.Of<IOrderService>());
        TestHelper.SetupController(controller);

        var result = await controller.Details("o1");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        redirect.ActionName.Should().Be("Login");
    }
}
