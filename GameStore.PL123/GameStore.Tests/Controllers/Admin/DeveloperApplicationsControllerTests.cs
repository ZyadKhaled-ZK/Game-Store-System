using FluentAssertions;
using GameStore.PL.Areas.Admin.Controllers;
using GameStore.PL.Services;
using Moq;

namespace GameStore.Tests.Controllers.Admin;

public class DeveloperApplicationsControllerTests
{
    [Fact]
    public async Task Index_Returns_View_With_All()
    {
        var svc = new Mock<IDeveloperApplicationService>();
        svc.Setup(s => s.GetAllAsync()).ReturnsAsync([
            new DeveloperApplication { Id = "a1", UserId = "u1", Name = "Studio", Status = ApplicationStatus.Pending }
        ]);

        var controller = new DeveloperApplicationsController(svc.Object, Mock.Of<INotificationService>());
        TestHelper.SetupController(controller);

        var result = await controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        var apps = Assert.IsAssignableFrom<List<DeveloperApplication>>(view.Model);
        apps.Should().HaveCount(1);
    }

    [Fact]
    public async Task Approve_Redirects_With_Success()
    {
        var svc = new Mock<IDeveloperApplicationService>();
        svc.Setup(s => s.GetByIdAsync("a1")).ReturnsAsync(
            new DeveloperApplication { Id = "a1", UserId = "u1", Name = "Studio" });
        svc.Setup(s => s.ApproveAsync("a1")).ReturnsAsync((true, ""));

        var controller = new DeveloperApplicationsController(svc.Object, Mock.Of<INotificationService>());
        TestHelper.SetupController(controller);

        var result = await controller.Approve("a1");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        controller.TempData["Message"].Should().Be("Developer application approved.");
    }

    [Fact]
    public async Task Approve_Redirects_With_Error_When_Not_Found()
    {
        var svc = new Mock<IDeveloperApplicationService>();
        svc.Setup(s => s.GetByIdAsync("nonexistent")).ReturnsAsync((DeveloperApplication?)null);

        var controller = new DeveloperApplicationsController(svc.Object, Mock.Of<INotificationService>());
        TestHelper.SetupController(controller);

        var result = await controller.Approve("nonexistent");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        controller.TempData["Message"].Should().Be("Application not found.");
        controller.TempData["IsError"].Should().Be(true);
    }

    [Fact]
    public async Task Reject_Redirects_With_Success()
    {
        var svc = new Mock<IDeveloperApplicationService>();
        svc.Setup(s => s.GetByIdAsync("a1")).ReturnsAsync(
            new DeveloperApplication { Id = "a1", UserId = "u1", Name = "Studio" });
        svc.Setup(s => s.RejectAsync("a1")).ReturnsAsync((true, ""));

        var controller = new DeveloperApplicationsController(svc.Object, Mock.Of<INotificationService>());
        TestHelper.SetupController(controller);

        var result = await controller.Reject("a1");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        controller.TempData["Message"].Should().Be("Developer application rejected.");
    }
}
