using FluentAssertions;
using Moq;

namespace GameStore.Tests.Controllers;

public class WishlistControllerTests
{
    [Fact]
    public async Task Index_Redirects_To_Login_When_Not_Logged_In()
    {
        var controller = new GameStore.PL.Controllers.WishlistController(Mock.Of<IWishlistService>(), Mock.Of<ICartService>());
        TestHelper.SetupController(controller);

        var result = await controller.Index();

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        redirect.ActionName.Should().Be("Login");
        redirect.ControllerName.Should().Be("Auth");
    }

    [Fact]
    public async Task Index_Returns_View_When_Logged_In()
    {
        var wishlistSvc = new Mock<IWishlistService>();
        wishlistSvc.Setup(s => s.GetWishlistAsync("u1")).ReturnsAsync(new List<WishlistItem>());

        var controller = new GameStore.PL.Controllers.WishlistController(wishlistSvc.Object, Mock.Of<ICartService>());
        TestHelper.SetupController(controller, userId: "u1");

        var result = await controller.Index();

        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task Remove_Redirects_To_Login_When_Not_Logged_In()
    {
        var controller = new GameStore.PL.Controllers.WishlistController(Mock.Of<IWishlistService>(), Mock.Of<ICartService>());
        TestHelper.SetupController(controller);

        var result = await controller.Remove("w1");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        redirect.ActionName.Should().Be("Login");
    }

    [Fact]
    public async Task Remove_Redirects_With_Success()
    {
        var wishlistSvc = new Mock<IWishlistService>();
        wishlistSvc.Setup(s => s.RemoveFromWishlistAsync("w1", "u1")).ReturnsAsync(true);

        var controller = new GameStore.PL.Controllers.WishlistController(wishlistSvc.Object, Mock.Of<ICartService>());
        TestHelper.SetupController(controller, userId: "u1");

        var result = await controller.Remove("w1");

        Assert.IsType<RedirectToActionResult>(result);
    }

    [Fact]
    public async Task Remove_Redirects_With_Error_When_Id_Empty()
    {
        var controller = new GameStore.PL.Controllers.WishlistController(Mock.Of<IWishlistService>(), Mock.Of<ICartService>());
        TestHelper.SetupController(controller, userId: "u1");

        var result = await controller.Remove("");

        Assert.IsType<RedirectToActionResult>(result);
        controller.TempData["IsError"].Should().Be(true);
    }

    [Fact]
    public async Task ToggleWishlist_Returns_Json_When_Not_Logged_In()
    {
        var controller = new GameStore.PL.Controllers.WishlistController(Mock.Of<IWishlistService>(), Mock.Of<ICartService>());
        TestHelper.SetupController(controller);

        var result = await controller.ToggleWishlist("g1");

        var json = Assert.IsType<JsonResult>(result);
        Assert.False(json.GetJsonBool("success"));
    }

    [Fact]
    public async Task AddToCart_Redirects_To_Login_When_Not_Logged_In()
    {
        var controller = new GameStore.PL.Controllers.WishlistController(Mock.Of<IWishlistService>(), Mock.Of<ICartService>());
        TestHelper.SetupController(controller);

        var result = await controller.AddToCart("g1");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        redirect.ActionName.Should().Be("Login");
    }
}
