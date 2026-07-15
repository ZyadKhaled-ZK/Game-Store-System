using FluentAssertions;
using GameStore.PL.Areas.Admin.Controllers;
using GameStore.PL.Services;
using Moq;

namespace GameStore.Tests.Controllers.Admin;

public class SalesControllerTests
{
    [Fact]
    public async Task Index_Returns_View_With_Pending_Sales()
    {
        var saleSvc = new Mock<ISaleService>();
        saleSvc.Setup(s => s.GetPendingAsync()).ReturnsAsync([
            new Sale { Id = "s1", GameId = "g1", NewPrice = 5m, Status = SaleStatus.Pending }
        ]);

        var controller = new SalesController(saleSvc.Object, Mock.Of<IDeveloperService>(), Mock.Of<INotificationService>());
        TestHelper.SetupController(controller);

        var result = await controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        var sales = Assert.IsAssignableFrom<List<Sale>>(view.Model);
        sales.Should().HaveCount(1);
    }

    [Fact]
    public async Task Approve_Redirects_With_Success()
    {
        var saleSvc = new Mock<ISaleService>();
        var devSvc = new Mock<IDeveloperService>();
        var notifSvc = new Mock<INotificationService>();

        var sale = new Sale
        {
            Id = "s1", GameId = "g1", DeveloperId = "d1", NewPrice = 5m,
            Status = SaleStatus.Pending, StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(7),
            Developer = new Developer { Id = "d1", Name = "Dev", UserId = "u1", Slug = "dev" },
            Game = new Game { Id = "g1", Title = "Game", Price = 10m, ReleaseDate = DateTime.UtcNow }
        };
        saleSvc.Setup(s => s.GetByIdAsync("s1")).ReturnsAsync(sale);
        saleSvc.Setup(s => s.ApproveAsync("s1")).ReturnsAsync((true, ""));

        var controller = new SalesController(saleSvc.Object, devSvc.Object, notifSvc.Object);
        TestHelper.SetupController(controller);

        var result = await controller.Approve("s1");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        redirect.ActionName.Should().Be("Index");
        controller.TempData["Message"].Should().Be("Sale approved.");
    }

    [Fact]
    public async Task Approve_Redirects_With_Error_When_NotFound()
    {
        var saleSvc = new Mock<ISaleService>();
        saleSvc.Setup(s => s.GetByIdAsync("nonexistent")).ReturnsAsync((Sale?)null);

        var controller = new SalesController(saleSvc.Object, Mock.Of<IDeveloperService>(), Mock.Of<INotificationService>());
        TestHelper.SetupController(controller);

        var result = await controller.Approve("nonexistent");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        controller.TempData["Message"].Should().Be("Sale not found.");
        controller.TempData["IsError"].Should().Be(true);
    }

    [Fact]
    public async Task Reject_Redirects_With_Success()
    {
        var saleSvc = new Mock<ISaleService>();
        var sale = new Sale
        {
            Id = "s1", GameId = "g1", DeveloperId = "d1", NewPrice = 5m,
            Status = SaleStatus.Pending, StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(7),
            Developer = new Developer { Id = "d1", Name = "Dev", UserId = "u1", Slug = "dev" },
            Game = new Game { Id = "g1", Title = "Game", Price = 10m, ReleaseDate = DateTime.UtcNow }
        };
        saleSvc.Setup(s => s.GetByIdAsync("s1")).ReturnsAsync(sale);
        saleSvc.Setup(s => s.RejectAsync("s1", "Too cheap")).ReturnsAsync((true, ""));

        var controller = new SalesController(saleSvc.Object, Mock.Of<IDeveloperService>(), Mock.Of<INotificationService>());
        TestHelper.SetupController(controller);

        var result = await controller.Reject("s1", "Too cheap");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        controller.TempData["Message"].Should().Be("Sale rejected.");
    }
}
