using AutoMapper;
using FluentAssertions;
using GameStore.PL.Hubs;
using Moq;

namespace GameStore.Tests.Controllers;

public class ProfileControllerTests
{
    [Fact]
    public async Task Index_Returns_View_When_Logged_In()
    {
        var userSvc = new Mock<IUserService>();
        userSvc.Setup(s => s.GetUserByUsernameAsync("Alice")).ReturnsAsync(
            new User { Id = "u1", Username = "Alice", Email = "a@t.com", PasswordHash = "h" });

        var controller = new GameStore.PL.Controllers.ProfileController(
            Mock.Of<IAuthService>(), userSvc.Object, Mock.Of<IPostService>(),
            Mock.Of<IFriendService>(), Mock.Of<IChatService>(), new ConnectionTracker(),
            Mock.Of<IDeveloperService>(), Mock.Of<ISaleService>(), Mock.Of<IMapper>());
        TestHelper.SetupController(controller, userId: "u1", username: "Alice");

        var result = await controller.Index("Alice");

        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task Index_Redirects_When_Null_Username()
    {
        var userSvc = new Mock<IUserService>();

        var controller = new GameStore.PL.Controllers.ProfileController(
            Mock.Of<IAuthService>(), userSvc.Object, Mock.Of<IPostService>(),
            Mock.Of<IFriendService>(), Mock.Of<IChatService>(), new ConnectionTracker(),
            Mock.Of<IDeveloperService>(), Mock.Of<ISaleService>(), Mock.Of<IMapper>());
        TestHelper.SetupController(controller);

        var result = await controller.Index(null);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        redirect.ActionName.Should().Be("Index");
        redirect.ControllerName.Should().Be("Home");
    }

    [Fact]
    public async Task Edit_Returns_View()
    {
        var userSvc = new Mock<IUserService>();
        userSvc.Setup(s => s.GetByIdAsync("u1")).ReturnsAsync(
            new User { Id = "u1", Username = "Alice", Email = "a@t.com", PasswordHash = "h" });

        var chatSvc = new Mock<IChatService>();
        chatSvc.Setup(s => s.GetConversationsAsync("u1")).ReturnsAsync(
            new List<(string UserId, string Username, Message? LastMessage, int UnreadCount)>());

        var controller = new GameStore.PL.Controllers.ProfileController(
            Mock.Of<IAuthService>(), userSvc.Object, Mock.Of<IPostService>(),
            Mock.Of<IFriendService>(), chatSvc.Object, new ConnectionTracker(),
            Mock.Of<IDeveloperService>(), Mock.Of<ISaleService>(), Mock.Of<IMapper>());
        TestHelper.SetupController(controller, userId: "u1", username: "Alice");

        var result = await controller.Edit();

        Assert.IsType<ViewResult>(result);
    }
}
