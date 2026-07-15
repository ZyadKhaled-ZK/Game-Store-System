using FluentAssertions;
using GameStore.PL.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Moq;

namespace GameStore.Tests.Controllers;

public class LibraryControllerTests
{
    [Fact]
    public async Task Index_Returns_View_When_Logged_In()
    {
        var libSvc = new Mock<ILibraryService>();
        var gameSvc = new Mock<IGameService>();
        var gameAccess = new Mock<IGameAccessService>();
        var gameVerSvc = new Mock<IGameVersionService>();
        var creditSvc = new Mock<ICreditService>();
        var env = Mock.Of<IWebHostEnvironment>();
        var logger = Mock.Of<ILogger<GameStore.PL.Controllers.LibraryController>>();

        libSvc.Setup(s => s.GetLibraryGamesAsync("u1")).ReturnsAsync(new List<LibraryGame>());
        gameAccess.Setup(s => s.GetPreviewableGameIdsAsync("u1", DAL.Enum.Role.CUSTOMER)).ReturnsAsync(new HashSet<string>());
        creditSvc.Setup(s => s.GetAvailableBalanceAsync("u1")).ReturnsAsync(0m);

        var controller = new GameStore.PL.Controllers.LibraryController(libSvc.Object, gameSvc.Object,
            gameAccess.Object, env, logger, gameVerSvc.Object, creditSvc.Object);
        TestHelper.SetupController(controller, userId: "u1", role: "CUSTOMER");

        var result = await controller.Index();

        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task Index_Returns_View_When_Not_Logged_In()
    {
        var controller = new GameStore.PL.Controllers.LibraryController(
            Mock.Of<ILibraryService>(), Mock.Of<IGameService>(), Mock.Of<IGameAccessService>(),
            Mock.Of<IWebHostEnvironment>(), Mock.Of<ILogger<GameStore.PL.Controllers.LibraryController>>(),
            Mock.Of<IGameVersionService>(), Mock.Of<ICreditService>());
        TestHelper.SetupController(controller);

        var result = await controller.Index();

        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task Download_Redirects_To_Login_When_Not_Logged_In()
    {
        var controller = new GameStore.PL.Controllers.LibraryController(
            Mock.Of<ILibraryService>(), Mock.Of<IGameService>(), Mock.Of<IGameAccessService>(),
            Mock.Of<IWebHostEnvironment>(), Mock.Of<ILogger<GameStore.PL.Controllers.LibraryController>>(),
            Mock.Of<IGameVersionService>(), Mock.Of<ICreditService>());
        TestHelper.SetupController(controller);

        var result = await controller.Download("g1");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        redirect.ActionName.Should().Be("Login");
        redirect.ControllerName.Should().Be("Auth");
    }
}
