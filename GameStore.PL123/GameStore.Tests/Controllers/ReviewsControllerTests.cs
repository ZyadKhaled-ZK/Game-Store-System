using FluentAssertions;
using GameStore.PL.Models.Home;
using Moq;

namespace GameStore.Tests.Controllers;

public class ReviewsControllerTests
{
    [Fact]
    public async Task AddReview_Returns_Json_When_Valid()
    {
        var svc = new Mock<IReviewService>();
        svc.Setup(s => s.CreateAsync("u1", "g1", 4, "Great!")).ReturnsAsync((true, ""));

        var controller = new GameStore.PL.Controllers.ReviewsController(svc.Object);
        TestHelper.SetupController(controller, userId: "u1");

        var result = await controller.AddReview(new ReviewRequest { GameId = "g1", Rating = 4, Comment = "Great!" });

        var json = Assert.IsType<JsonResult>(result);
        Assert.True(json.GetJsonBool("success"));
    }

    [Fact]
    public async Task AddReview_Returns_Json_Error_When_Not_Logged_In()
    {
        var controller = new GameStore.PL.Controllers.ReviewsController(Mock.Of<IReviewService>());
        TestHelper.SetupController(controller);

        var result = await controller.AddReview(new ReviewRequest { GameId = "g1", Rating = 4 });

        var json = Assert.IsType<JsonResult>(result);
        Assert.False(json.GetJsonBool("success"));
    }

    [Fact]
    public async Task AddReview_Returns_Json_When_Service_Fails()
    {
        var svc = new Mock<IReviewService>();
        svc.Setup(s => s.CreateAsync("u1", "g1", 4, null)).ReturnsAsync((false, "Not owned."));

        var controller = new GameStore.PL.Controllers.ReviewsController(svc.Object);
        TestHelper.SetupController(controller, userId: "u1");

        var result = await controller.AddReview(new ReviewRequest { GameId = "g1", Rating = 4 });

        var json = Assert.IsType<JsonResult>(result);
        Assert.False(json.GetJsonBool("success"));
    }
}
