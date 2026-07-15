using FluentAssertions;
using GameStore.DAL.Repo;
using GameStore.PL.Services;
using Moq;

namespace GameStore.Tests.Controllers;

public class NotificationsControllerTests
{
    [Fact]
    public void Index_Redirects_To_Home()
    {
        var controller = new GameStore.PL.Controllers.NotificationsController(
            Mock.Of<IUnitOfWork>(), Mock.Of<ISaleService>(), Mock.Of<IDeveloperApplicationService>());
        TestHelper.SetupController(controller, userId: "u1", username: "Alice");

        var result = controller.Index();

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        redirect.ActionName.Should().Be("Index");
        redirect.ControllerName.Should().Be("Home");
    }

    [Fact]
    public async Task GetUnreadCount_Returns_Json()
    {
        var uow = new Mock<IUnitOfWork>();
        var notifRepo = new Mock<IRepository<UserNotification>>();
        notifRepo.Setup(r => r.Query()).Returns(new List<UserNotification>().BuildMock());
        uow.Setup(u => u.Repository<UserNotification>()).Returns(notifRepo.Object);

        var saleRepo = new Mock<IRepository<Sale>>();
        saleRepo.Setup(r => r.Query()).Returns(new List<Sale>().BuildMock());
        uow.Setup(u => u.Repository<Sale>()).Returns(saleRepo.Object);

        var devAppRepo = new Mock<IRepository<DeveloperApplication>>();
        devAppRepo.Setup(r => r.Query()).Returns(new List<DeveloperApplication>().BuildMock());
        uow.Setup(u => u.Repository<DeveloperApplication>()).Returns(devAppRepo.Object);

        var controller = new GameStore.PL.Controllers.NotificationsController(
            uow.Object, Mock.Of<ISaleService>(), Mock.Of<IDeveloperApplicationService>());
        TestHelper.SetupController(controller, userId: "u1");

        var result = await controller.GetUnreadCount();

        Assert.IsType<JsonResult>(result);
    }

    [Fact]
    public async Task GetUnreadCount_Returns_Zero_When_Not_Logged_In()
    {
        var controller = new GameStore.PL.Controllers.NotificationsController(
            Mock.Of<IUnitOfWork>(), Mock.Of<ISaleService>(), Mock.Of<IDeveloperApplicationService>());
        TestHelper.SetupController(controller);

        var result = await controller.GetUnreadCount();

        var json = Assert.IsType<JsonResult>(result);
        Assert.Equal(0, json.GetJsonInt("unread"));
    }

    [Fact]
    public async Task MarkAsRead_Returns_Ok()
    {
        var uow = new Mock<IUnitOfWork>();
        var notifRepo = new Mock<IRepository<UserNotification>>();
        notifRepo.Setup(r => r.GetByIdAsync("n1")).ReturnsAsync(
            new UserNotification { Id = "n1", UserId = "u1" });
        uow.Setup(u => u.Repository<UserNotification>()).Returns(notifRepo.Object);

        var controller = new GameStore.PL.Controllers.NotificationsController(
            uow.Object, Mock.Of<ISaleService>(), Mock.Of<IDeveloperApplicationService>());
        TestHelper.SetupController(controller, userId: "u1");

        var result = await controller.MarkAsRead("n1");

        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task MarkAllAsRead_Returns_Ok()
    {
        var uow = new Mock<IUnitOfWork>();
        var notifRepo = new Mock<IRepository<UserNotification>>();
        notifRepo.Setup(r => r.Query()).Returns(new List<UserNotification>().BuildMock());
        uow.Setup(u => u.Repository<UserNotification>()).Returns(notifRepo.Object);

        var msgRepo = new Mock<IRepository<Message>>();
        msgRepo.Setup(r => r.Query()).Returns(new List<Message>().BuildMock());
        uow.Setup(u => u.Repository<Message>()).Returns(msgRepo.Object);

        var controller = new GameStore.PL.Controllers.NotificationsController(
            uow.Object, Mock.Of<ISaleService>(), Mock.Of<IDeveloperApplicationService>());
        TestHelper.SetupController(controller, userId: "u1");

        var result = await controller.MarkAllAsRead();

        Assert.IsType<OkResult>(result);
    }
}
