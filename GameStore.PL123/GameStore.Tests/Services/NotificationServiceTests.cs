using FluentAssertions;
using GameStore.Tests.Controllers;

namespace GameStore.Tests.Services;

public class NotificationServiceTests
{
    private static GameStoreDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<GameStoreDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new GameStoreDbContext(options);
    }

    [Fact]
    public async Task SendToUserAsync_CreatesNotificationInDatabase()
    {
        using var ctx = CreateContext("Notif_SendUser");
        var uow = new UnitOfWork(ctx);
        var hubMock = new Mock<IHubContext<NotificationHub>>();
        hubMock.Setup(h => h.Clients.Group(It.IsAny<string>()))
            .Returns(new Mock<IClientProxy>().Object);
        var service = new NotificationService(hubMock.Object, uow);

        await service.SendToUserAsync("u1", "Test Title", "Test Message", "info", "General", null, null, null);

        var notif = ctx.UserNotifications.Single();
        notif.UserId.Should().Be("u1");
        notif.Title.Should().Be("Test Title");
        notif.Message.Should().Be("Test Message");
        notif.Type.Should().Be("info");
        notif.Category.Should().Be("General");
    }

    [Fact]
    public async Task SendToUserAsync_SendsViaSignalR()
    {
        using var ctx = CreateContext("Notif_SignalR");
        var uow = new UnitOfWork(ctx);
        var clientProxyMock = new Mock<IClientProxy>();
        var hubMock = new Mock<IHubContext<NotificationHub>>();
        hubMock.Setup(h => h.Clients.Group("u1")).Returns(clientProxyMock.Object);
        var service = new NotificationService(hubMock.Object, uow);

        await service.SendToUserAsync("u1", "Title", "Msg", "info");

        hubMock.Verify(h => h.Clients.Group("u1"), Times.Once);
    }

    [Fact]
    public async Task SendToUserAsync_WithCategory_DefaultsCategoryToGeneral()
    {
        using var ctx = CreateContext("Notif_Category");
        var uow = new UnitOfWork(ctx);
        var hubMock = new Mock<IHubContext<NotificationHub>>();
        hubMock.Setup(h => h.Clients.Group(It.IsAny<string>()))
            .Returns(new Mock<IClientProxy>().Object);
        var service = new NotificationService(hubMock.Object, uow);

        await service.SendToUserAsync("u1", "T", "M", "info");

        var notif = ctx.UserNotifications.Single();
        notif.Category.Should().Be("General");
    }

    [Fact]
    public async Task SendToAdminsAsync_CreatesNotificationWithNullUserId()
    {
        using var ctx = CreateContext("Notif_Admins");
        var uow = new UnitOfWork(ctx);
        var hubMock = new Mock<IHubContext<NotificationHub>>();
        hubMock.Setup(h => h.Clients.Group("Admins"))
            .Returns(new Mock<IClientProxy>().Object);
        var service = new NotificationService(hubMock.Object, uow);

        await service.SendToAdminsAsync("Admin Alert", "Something happened", "alert", "system", null, "/admin/dashboard");

        var notif = ctx.UserNotifications.Single();
        notif.UserId.Should().BeNull();
        notif.Title.Should().Be("Admin Alert");
        notif.ReferenceUrl.Should().Be("/admin/dashboard");
    }

    [Fact]
    public async Task SendToAdminsAsync_SendsToAdminsGroup()
    {
        using var ctx = CreateContext("Notif_AdminsSignalR");
        var uow = new UnitOfWork(ctx);
        var clientProxyMock = new Mock<IClientProxy>();
        var hubMock = new Mock<IHubContext<NotificationHub>>();
        hubMock.Setup(h => h.Clients.Group("Admins")).Returns(clientProxyMock.Object);
        var service = new NotificationService(hubMock.Object, uow);

        await service.SendToAdminsAsync("T", "M", "info");

        hubMock.Verify(h => h.Clients.Group("Admins"), Times.Once);
    }

    [Fact]
    public async Task SendToUserAsync_MultipleNotifications_AllPersisted()
    {
        using var ctx = CreateContext("Notif_Multiple");
        var uow = new UnitOfWork(ctx);
        var hubMock = new Mock<IHubContext<NotificationHub>>();
        hubMock.Setup(h => h.Clients.Group(It.IsAny<string>()))
            .Returns(new Mock<IClientProxy>().Object);
        var service = new NotificationService(hubMock.Object, uow);

        await service.SendToUserAsync("u1", "Title1", "Msg1", "info");
        await service.SendToUserAsync("u1", "Title2", "Msg2", "warning");
        await service.SendToUserAsync("u1", "Title3", "Msg3", "error");

        ctx.UserNotifications.Should().HaveCount(3);
    }
}
