using FluentAssertions;
using GameStore.DAL.Entities;
using GameStore.DAL.Repo;
using GameStore.PL.Hubs;
using GameStore.PL.Services;
using Moq;

namespace GameStore.Tests.Controllers;

public class FriendsControllerTests
{
    [Fact]
    public async Task Index_Returns_View_When_Logged_In()
    {
        var friendSvc = new Mock<IFriendService>();
        var suggestionSvc = new Mock<IFriendSuggestionService>();
        var userSvc = new Mock<IUserService>();
        var chatSvc = new Mock<IChatService>();

        friendSvc.Setup(s => s.GetFriendsAsync("u1")).ReturnsAsync(new List<Friendship>());
        friendSvc.Setup(s => s.GetPendingRequestsAsync("u1")).ReturnsAsync(new List<Friendship>());
        suggestionSvc.Setup(s => s.GetSuggestionsAsync("u1", 6)).ReturnsAsync(new List<(User User, int MutualGamesCount)>());
        chatSvc.Setup(s => s.GetConversationsAsync("u1")).ReturnsAsync(new List<(string UserId, string Username, Message? LastMessage, int UnreadCount)>());

        var controller = new GameStore.PL.Controllers.FriendsController(friendSvc.Object, suggestionSvc.Object,
            userSvc.Object, chatSvc.Object, new ConnectionTracker(),
            Mock.Of<INotificationService>(), Mock.Of<IUnitOfWork>());
        TestHelper.SetupController(controller, userId: "u1", username: "Alice");

        var result = await controller.Index(null);

        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task Index_Redirects_When_Not_Logged_In()
    {
        var controller = new GameStore.PL.Controllers.FriendsController(Mock.Of<IFriendService>(),
            Mock.Of<IFriendSuggestionService>(), Mock.Of<IUserService>(), Mock.Of<IChatService>(),
            new ConnectionTracker(), Mock.Of<INotificationService>(), Mock.Of<IUnitOfWork>());
        TestHelper.SetupController(controller);

        var result = await controller.Index(null);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        redirect.ActionName.Should().Be("Login");
    }

    [Fact]
    public async Task SendRequest_Returns_Json()
    {
        var friendSvc = new Mock<IFriendService>();
        friendSvc.Setup(s => s.SendRequestAsync("u1", "Bob")).ReturnsAsync((true, "Friend request sent."));

        var controller = new GameStore.PL.Controllers.FriendsController(friendSvc.Object,
            Mock.Of<IFriendSuggestionService>(), Mock.Of<IUserService>(), Mock.Of<IChatService>(),
            new ConnectionTracker(), Mock.Of<INotificationService>(), Mock.Of<IUnitOfWork>());
        TestHelper.SetupController(controller, userId: "u1");

        var result = await controller.SendRequest("Bob");

        var json = Assert.IsType<JsonResult>(result);
        Assert.True(json.GetJsonBool("success"));
    }

    [Fact]
    public async Task AcceptRequest_Returns_Json()
    {
        var friendSvc = new Mock<IFriendService>();
        friendSvc.Setup(s => s.AcceptRequestAsync("f1", "u1")).ReturnsAsync((true, "Friend request accepted!"));

        var uow = new Mock<IUnitOfWork>();
        var friendshipRepo = new Mock<IRepository<Friendship>>();
        friendshipRepo.Setup(r => r.Query()).Returns(new List<Friendship>().BuildMock());
        uow.Setup(u => u.Repository<Friendship>()).Returns(friendshipRepo.Object);

        var controller = new GameStore.PL.Controllers.FriendsController(friendSvc.Object,
            Mock.Of<IFriendSuggestionService>(), Mock.Of<IUserService>(), Mock.Of<IChatService>(),
            new ConnectionTracker(), Mock.Of<INotificationService>(), uow.Object);
        TestHelper.SetupController(controller, userId: "u1");

        var result = await controller.AcceptRequest("f1");

        var json = Assert.IsType<JsonResult>(result);
        Assert.True(json.GetJsonBool("success"));
    }

    [Fact]
    public async Task RejectRequest_Returns_Json()
    {
        var friendSvc = new Mock<IFriendService>();
        friendSvc.Setup(s => s.RejectRequestAsync("f1", "u1")).ReturnsAsync((true, "Friend request rejected."));

        var controller = new GameStore.PL.Controllers.FriendsController(friendSvc.Object,
            Mock.Of<IFriendSuggestionService>(), Mock.Of<IUserService>(), Mock.Of<IChatService>(),
            new ConnectionTracker(), Mock.Of<INotificationService>(), Mock.Of<IUnitOfWork>());
        TestHelper.SetupController(controller, userId: "u1");

        var result = await controller.RejectRequest("f1");

        var json = Assert.IsType<JsonResult>(result);
        Assert.True(json.GetJsonBool("success"));
    }

    [Fact]
    public async Task RemoveFriend_Returns_Json()
    {
        var friendSvc = new Mock<IFriendService>();
        friendSvc.Setup(s => s.RemoveFriendAsync("f1", "u1")).ReturnsAsync((true, "Friend removed."));

        var controller = new GameStore.PL.Controllers.FriendsController(friendSvc.Object,
            Mock.Of<IFriendSuggestionService>(), Mock.Of<IUserService>(), Mock.Of<IChatService>(),
            new ConnectionTracker(), Mock.Of<INotificationService>(), Mock.Of<IUnitOfWork>());
        TestHelper.SetupController(controller, userId: "u1");

        var result = await controller.RemoveFriend("f1");

        var json = Assert.IsType<JsonResult>(result);
        Assert.True(json.GetJsonBool("success"));
    }
}
