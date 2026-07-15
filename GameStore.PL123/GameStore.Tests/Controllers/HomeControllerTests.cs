using FluentAssertions;
using AutoMapper;
using GameStore.BLL.Models;
using GameStore.PL.Controllers;
using GameStore.PL.Models.Home;
using GameStore.PL.Services;
using Moq;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace GameStore.Tests.Controllers;

public class HomeControllerTests
{
    [Fact]
    public async Task Index_Returns_View()
    {
        var gameSvc = new Mock<IGameService>();
        var catSvc = new Mock<ICategoryService>();
        var cartSvc = new Mock<ICartService>();
        var wishlistSvc = new Mock<IWishlistService>();
        var libSvc = new Mock<ILibraryService>();
        var saleSvc = new Mock<ISaleService>();
        var reviewSvc = new Mock<IReviewService>();
        var mapper = new Mock<IMapper>();
        var gameAccess = new Mock<IGameAccessService>();
        var sysReqSvc = new Mock<ISystemRequirementService>();
        var gameVerSvc = new Mock<IGameVersionService>();

        gameSvc.Setup(s => s.GetPagedAsync(1, 4, null)).ReturnsAsync(new PagedResult<Game>
        {
            Items = [new Game { Id = "g1", Title = "Game", Price = 10m, ReleaseDate = DateTime.UtcNow }],
            Page = 1, PageSize = 4, TotalCount = 1
        });
        gameSvc.Setup(s => s.GetHeroGamesAsync(5)).ReturnsAsync(new List<Game>());
        catSvc.Setup(s => s.GetAllAsync()).ReturnsAsync(new List<Category>());
        saleSvc.Setup(s => s.GetActiveSalesByGameIdsAsync(It.IsAny<List<string>>())).ReturnsAsync(new List<Sale>());
        reviewSvc.Setup(s => s.GetByGameIdsAsync(It.IsAny<List<string>>())).ReturnsAsync(new List<Review>());

        var logger = Mock.Of<ILogger<HomeController>>();

        var controller = new HomeController(logger, gameSvc.Object, catSvc.Object, cartSvc.Object,
            wishlistSvc.Object, libSvc.Object, saleSvc.Object, reviewSvc.Object,
            mapper.Object, gameAccess.Object, sysReqSvc.Object, gameVerSvc.Object,
            Mock.Of<IDistributedCache>());
        TestHelper.SetupController(controller);

        var result = await controller.Index(null, null);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<HomeViewModel>(view.Model);
        model.Games.Should().HaveCount(1);
    }

    [Fact]
    public void Privacy_Returns_View()
    {
        var logger = Mock.Of<ILogger<HomeController>>();
        var controller = new HomeController(logger, Mock.Of<IGameService>(), Mock.Of<ICategoryService>(),
            Mock.Of<ICartService>(), Mock.Of<IWishlistService>(), Mock.Of<ILibraryService>(),
            Mock.Of<ISaleService>(), Mock.Of<IReviewService>(), Mock.Of<IMapper>(),
            Mock.Of<IGameAccessService>(), Mock.Of<ISystemRequirementService>(), Mock.Of<IGameVersionService>(),
            Mock.Of<IDistributedCache>());
        TestHelper.SetupController(controller);

        var result = controller.Privacy();

        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public void Error_Returns_View()
    {
        var logger = Mock.Of<ILogger<HomeController>>();
        var controller = new HomeController(logger, Mock.Of<IGameService>(), Mock.Of<ICategoryService>(),
            Mock.Of<ICartService>(), Mock.Of<IWishlistService>(), Mock.Of<ILibraryService>(),
            Mock.Of<ISaleService>(), Mock.Of<IReviewService>(), Mock.Of<IMapper>(),
            Mock.Of<IGameAccessService>(), Mock.Of<ISystemRequirementService>(), Mock.Of<IGameVersionService>(),
            Mock.Of<IDistributedCache>());
        TestHelper.SetupController(controller);

        var result = controller.Error();

        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task GetRequirements_Returns_Json()
    {
        var sysReqSvc = new Mock<ISystemRequirementService>();
        sysReqSvc.Setup(s => s.GetAsync("g1")).ReturnsAsync(new SystemRequirementsModel());

        var logger = Mock.Of<ILogger<HomeController>>();
        var controller = new HomeController(logger, Mock.Of<IGameService>(), Mock.Of<ICategoryService>(),
            Mock.Of<ICartService>(), Mock.Of<IWishlistService>(), Mock.Of<ILibraryService>(),
            Mock.Of<ISaleService>(), Mock.Of<IReviewService>(), Mock.Of<IMapper>(),
            Mock.Of<IGameAccessService>(), sysReqSvc.Object, Mock.Of<IGameVersionService>(),
            Mock.Of<IDistributedCache>());
        TestHelper.SetupController(controller);

        var result = await controller.GetRequirements("g1");

        Assert.IsType<JsonResult>(result);
    }

    [Fact]
    public async Task GetVersions_Returns_Json()
    {
        var gameVerSvc = new Mock<IGameVersionService>();
        gameVerSvc.Setup(s => s.GetAllAsync("g1")).ReturnsAsync(new List<GameVersionModel>());

        var logger = Mock.Of<ILogger<HomeController>>();
        var controller = new HomeController(logger, Mock.Of<IGameService>(), Mock.Of<ICategoryService>(),
            Mock.Of<ICartService>(), Mock.Of<IWishlistService>(), Mock.Of<ILibraryService>(),
            Mock.Of<ISaleService>(), Mock.Of<IReviewService>(), Mock.Of<IMapper>(),
            Mock.Of<IGameAccessService>(), Mock.Of<ISystemRequirementService>(), gameVerSvc.Object,
            Mock.Of<IDistributedCache>());
        TestHelper.SetupController(controller);

        var result = await controller.GetVersions("g1");

        Assert.IsType<JsonResult>(result);
    }
}
