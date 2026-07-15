using FluentAssertions;
using GameStore.PL.Areas.Admin.Controllers;
using GameStore.PL.Models.Admin;
using GameStore.PL.Services;
using Moq;

namespace GameStore.Tests.Controllers.Admin;

public class DevelopersControllerTests
{
    [Fact]
    public async Task Index_Returns_View_With_Developers()
    {
        var devSvc = new Mock<IDeveloperService>();
        devSvc.Setup(s => s.GetAllAsync()).ReturnsAsync([
            new Developer { Id = "d1", Name = "Studio", UserId = "u1", Slug = "studio" }
        ]);
        devSvc.Setup(s => s.GetGamesAsync("d1")).ReturnsAsync(new List<Game>());

        var controller = new DevelopersController(devSvc.Object, Mock.Of<IUserService>(), Mock.Of<INotificationService>());
        TestHelper.SetupController(controller);

        var result = await controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ManageDevelopersViewModel>(view.Model);
        model.Developers.Should().HaveCount(1);
    }

    [Fact]
    public async Task Details_Returns_View()
    {
        var devSvc = new Mock<IDeveloperService>();
        var dev = new Developer { Id = "d1", Name = "Studio", UserId = "u1", Slug = "studio" };
        devSvc.Setup(s => s.GetByIdAsync("d1")).ReturnsAsync(dev);
        devSvc.Setup(s => s.GetGamesAsync("d1")).ReturnsAsync(new List<Game>());
        devSvc.Setup(s => s.GetDashboardStatsAsync("d1")).ReturnsAsync(
            (GameCount: 0, TotalDownloads: 0, TotalReviews: 0, TotalRevenue: 0, NetRevenue: 0, AvgRating: 0.0));
        devSvc.Setup(s => s.GetGameStatsAsync("d1")).ReturnsAsync(new List<(Game Game, int Downloads, double AvgRating, int ReviewCount, decimal TotalRevenue)>());

        var controller = new DevelopersController(devSvc.Object, Mock.Of<IUserService>(), Mock.Of<INotificationService>());
        TestHelper.SetupController(controller);

        var result = await controller.Details("d1");

        var view = Assert.IsType<ViewResult>(result);
        Assert.IsType<Developer>(view.Model);
    }

    [Fact]
    public async Task Details_Redirects_When_Not_Found()
    {
        var devSvc = new Mock<IDeveloperService>();
        devSvc.Setup(s => s.GetByIdAsync("nonexistent")).ReturnsAsync((Developer?)null);

        var controller = new DevelopersController(devSvc.Object, Mock.Of<IUserService>(), Mock.Of<INotificationService>());
        TestHelper.SetupController(controller);

        var result = await controller.Details("nonexistent");

        Assert.IsType<RedirectToActionResult>(result);
    }

    [Fact]
    public async Task Demote_Redirects_With_Success()
    {
        var devSvc = new Mock<IDeveloperService>();
        var dev = new Developer { Id = "d1", Name = "Studio", UserId = "u1", Slug = "studio" };
        devSvc.Setup(s => s.GetByIdAsync("d1")).ReturnsAsync(dev);
        devSvc.Setup(s => s.DemoteAsync("d1", "admin")).ReturnsAsync((true, ""));

        var controller = new DevelopersController(devSvc.Object, Mock.Of<IUserService>(), Mock.Of<INotificationService>());
        TestHelper.SetupController(controller, userId: "admin");

        var result = await controller.Demote("d1");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        controller.TempData["Message"].Should().Be("Developer demoted to customer.");
    }

    [Fact]
    public async Task Delete_Redirects_With_Success()
    {
        var devSvc = new Mock<IDeveloperService>();
        devSvc.Setup(s => s.DeleteAsync("d1")).ReturnsAsync((true, ""));

        var controller = new DevelopersController(devSvc.Object, Mock.Of<IUserService>(), Mock.Of<INotificationService>());
        TestHelper.SetupController(controller);

        var result = await controller.Delete("d1");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        controller.TempData["Message"].Should().Be("Developer profile removed.");
    }

    [Fact]
    public async Task Reactivate_Redirects_With_Success()
    {
        var devSvc = new Mock<IDeveloperService>();
        var dev = new Developer { Id = "d1", Name = "Studio", UserId = "u1", Slug = "studio", IsActive = false };
        devSvc.Setup(s => s.GetByIdAsync("d1")).ReturnsAsync(dev);
        devSvc.Setup(s => s.ReactivateAsync("d1", "admin")).ReturnsAsync((true, ""));

        var controller = new DevelopersController(devSvc.Object, Mock.Of<IUserService>(), Mock.Of<INotificationService>());
        TestHelper.SetupController(controller, userId: "admin");

        var result = await controller.Reactivate("d1");

        Assert.IsType<RedirectToActionResult>(result);
        controller.TempData["Message"].Should().Be("Developer access restored.");
    }

    [Fact]
    public async Task Edit_GET_Returns_View_When_Found()
    {
        var devSvc = new Mock<IDeveloperService>();
        devSvc.Setup(s => s.GetByIdAsync("d1")).ReturnsAsync(
            new Developer { Id = "d1", Name = "Studio", UserId = "u1", Slug = "studio" });

        var controller = new DevelopersController(devSvc.Object, Mock.Of<IUserService>(), Mock.Of<INotificationService>());
        TestHelper.SetupController(controller);

        var result = await controller.Edit("d1");

        var view = Assert.IsType<ViewResult>(result);
        var dev = Assert.IsType<Developer>(view.Model);
        dev.Name.Should().Be("Studio");
    }

    [Fact]
    public async Task Edit_POST_Redirects_When_Name_Empty()
    {
        var controller = new DevelopersController(Mock.Of<IDeveloperService>(),
            Mock.Of<IUserService>(), Mock.Of<INotificationService>());
        TestHelper.SetupController(controller);

        var result = await controller.Edit("d1", "", null, null, null);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        controller.TempData["Message"].Should().Be("Studio name is required.");
        controller.TempData["IsError"].Should().Be(true);
    }

    [Fact]
    public async Task Edit_POST_Redirects_When_Dev_Not_Found()
    {
        var devSvc = new Mock<IDeveloperService>();
        devSvc.Setup(s => s.GetByIdAsync("nonexistent")).ReturnsAsync((Developer?)null);

        var controller = new DevelopersController(devSvc.Object, Mock.Of<IUserService>(), Mock.Of<INotificationService>());
        TestHelper.SetupController(controller);

        var result = await controller.Edit("nonexistent", "Name", null, null, null);

        Assert.IsType<RedirectToActionResult>(result);
    }

    [Fact]
    public async Task RefundsController_Index_Returns_View()
    {
        var refundSvc = new Mock<IRefundService>();
        var creditSvc = new Mock<ICreditService>();
        refundSvc.Setup(s => s.GetAllAsync()).ReturnsAsync(new List<RefundRequest>());

        var controller = new GameStore.PL.Areas.Admin.Controllers.RefundsController(refundSvc.Object, creditSvc.Object);
        TestHelper.SetupController(controller);

        var result = await controller.Index();

        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task RefundsController_Approve_Redirects()
    {
        var refundSvc = new Mock<IRefundService>();
        refundSvc.Setup(s => s.ApproveAsync("r1", "ok")).ReturnsAsync((true, "Refund approved."));

        var controller = new GameStore.PL.Areas.Admin.Controllers.RefundsController(refundSvc.Object, Mock.Of<ICreditService>());
        TestHelper.SetupController(controller);

        var result = await controller.Approve("r1", "ok");

        Assert.IsType<RedirectToActionResult>(result);
    }

    [Fact]
    public async Task RefundsController_Reject_Redirects()
    {
        var refundSvc = new Mock<IRefundService>();
        refundSvc.Setup(s => s.RejectAsync("r1", "no")).ReturnsAsync((true, "Refund rejected."));

        var controller = new GameStore.PL.Areas.Admin.Controllers.RefundsController(refundSvc.Object, Mock.Of<ICreditService>());
        TestHelper.SetupController(controller);

        var result = await controller.Reject("r1", "no");

        Assert.IsType<RedirectToActionResult>(result);
    }
}
