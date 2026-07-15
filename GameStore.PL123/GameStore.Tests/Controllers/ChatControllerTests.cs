using FluentAssertions;
using GameStore.PL.Hubs;
using Moq;

namespace GameStore.Tests.Controllers;

public class ChatControllerTests
{
    [Fact]
    public async Task Index_Returns_View_When_Logged_In()
    {
        var chatSvc = new Mock<IChatService>();
        chatSvc.Setup(s => s.GetConversationsAsync("u1")).ReturnsAsync(
            new List<(string UserId, string Username, Message? LastMessage, int UnreadCount)>());

        var controller = new GameStore.PL.Controllers.ChatController(chatSvc.Object, Mock.Of<IFriendService>(), new ConnectionTracker());
        TestHelper.SetupController(controller, userId: "u1", username: "Alice");

        var result = await controller.Index(null);

        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task Index_Redirects_When_Not_Logged_In()
    {
        var controller = new GameStore.PL.Controllers.ChatController(Mock.Of<IChatService>(), Mock.Of<IFriendService>(), new ConnectionTracker());
        TestHelper.SetupController(controller);

        var result = await controller.Index(null);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        redirect.ActionName.Should().Be("Login");
    }

    [Fact]
    public async Task GetMessages_Returns_Json()
    {
        var chatSvc = new Mock<IChatService>();
        chatSvc.Setup(s => s.GetConversationAsync("u1", "u2", 1, 50)).ReturnsAsync(
            [new Message { Id = "m1", SenderId = "u1", ReceiverId = "u2", Content = "Hi" }]);

        var controller = new GameStore.PL.Controllers.ChatController(chatSvc.Object, Mock.Of<IFriendService>(), new ConnectionTracker());
        TestHelper.SetupController(controller, userId: "u1");

        var result = await controller.GetMessages("u2", 1);

        Assert.IsType<JsonResult>(result);
    }
}
