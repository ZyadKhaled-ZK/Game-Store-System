using FluentAssertions;
using Moq;

namespace GameStore.Tests.Controllers;

public class PostsControllerTests
{
    [Fact]
    public async Task Create_Returns_Json_Success()
    {
        var postSvc = new Mock<IPostService>();
        postSvc.Setup(s => s.CreateAsync("u1", "Hello!")).ReturnsAsync(
            new Post { Id = "p1", UserId = "u1", Content = "Hello!" });
        postSvc.Setup(s => s.GetLastPostTimeAsync("u1")).ReturnsAsync((DateTime?)null);

        var controller = new GameStore.PL.Controllers.PostsController(postSvc.Object);
        TestHelper.SetupController(controller, userId: "u1");

        var result = await controller.Create("Hello!");

        var json = Assert.IsType<JsonResult>(result);
        Assert.True(json.GetJsonBool("success"));
    }

    [Fact]
    public async Task Create_Returns_Json_Error_When_Not_Logged_In()
    {
        var controller = new GameStore.PL.Controllers.PostsController(Mock.Of<IPostService>());
        TestHelper.SetupController(controller);

        var result = await controller.Create("Hello!");

        var json = Assert.IsType<JsonResult>(result);
        Assert.False(json.GetJsonBool("success"));
    }

    [Fact]
    public async Task Create_Returns_Json_Error_When_Empty()
    {
        var controller = new GameStore.PL.Controllers.PostsController(Mock.Of<IPostService>());
        TestHelper.SetupController(controller, userId: "u1");

        var result = await controller.Create("");

        var json = Assert.IsType<JsonResult>(result);
        Assert.False(json.GetJsonBool("success"));
    }

    [Fact]
    public async Task Create_Returns_Json_Error_When_Too_Fast()
    {
        var postSvc = new Mock<IPostService>();
        postSvc.Setup(s => s.GetLastPostTimeAsync("u1")).ReturnsAsync(DateTime.UtcNow);

        var controller = new GameStore.PL.Controllers.PostsController(postSvc.Object);
        TestHelper.SetupController(controller, userId: "u1");

        var result = await controller.Create("Hello!");

        var json = Assert.IsType<JsonResult>(result);
        Assert.False(json.GetJsonBool("success"));
    }

    [Fact]
    public async Task Delete_Returns_Json_Success()
    {
        var postSvc = new Mock<IPostService>();
        postSvc.Setup(s => s.DeleteAsync("p1", "u1")).ReturnsAsync(true);

        var controller = new GameStore.PL.Controllers.PostsController(postSvc.Object);
        TestHelper.SetupController(controller, userId: "u1");

        var result = await controller.Delete("p1");

        var json = Assert.IsType<JsonResult>(result);
        Assert.True(json.GetJsonBool("success"));
    }

    [Fact]
    public async Task Delete_Returns_Json_Error_When_Not_Logged_In()
    {
        var controller = new GameStore.PL.Controllers.PostsController(Mock.Of<IPostService>());
        TestHelper.SetupController(controller);

        var result = await controller.Delete("p1");

        var json = Assert.IsType<JsonResult>(result);
        Assert.False(json.GetJsonBool("success"));
    }

    [Fact]
    public async Task Delete_Returns_Json_Error_When_Fails()
    {
        var postSvc = new Mock<IPostService>();
        postSvc.Setup(s => s.DeleteAsync("p1", "u2")).ReturnsAsync(false);

        var controller = new GameStore.PL.Controllers.PostsController(postSvc.Object);
        TestHelper.SetupController(controller, userId: "u2");

        var result = await controller.Delete("p1");

        var json = Assert.IsType<JsonResult>(result);
        Assert.False(json.GetJsonBool("success"));
    }
}
