using FluentAssertions;
using GameStore.PL.Areas.Admin.Controllers;
using GameStore.PL.Models.Admin;
using Moq;

namespace GameStore.Tests.Controllers.Admin;

public class UsersControllerTests
{
    [Fact]
    public async Task Index_Returns_View_With_Paged_Users()
    {
        var userSvc = new Mock<IUserService>();
        userSvc.Setup(s => s.GetAllAsync()).ReturnsAsync([
            new User { Id = "u1", Username = "Alice", Email = "a@t.com", PasswordHash = "h", Role = Role.CUSTOMER },
            new User { Id = "u2", Username = "Bob", Email = "b@t.com", PasswordHash = "h", Role = Role.ADMIN }
        ]);

        var controller = new UsersController(userSvc.Object, Mock.Of<IOrderService>(),
            Mock.Of<ILibraryService>(), Mock.Of<IReviewService>());
        TestHelper.SetupController(controller);

        var result = await controller.Index(1, 20);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ManageUsersViewModel>(view.Model);
        model.Users.Should().HaveCount(2);
    }

    [Fact]
    public async Task ChangeRole_Redirects_With_Success()
    {
        var userSvc = new Mock<IUserService>();
        userSvc.Setup(s => s.ChangeRoleAsync("u1", Role.ADMIN, "admin")).ReturnsAsync((true, ""));

        var controller = new UsersController(userSvc.Object, Mock.Of<IOrderService>(),
            Mock.Of<ILibraryService>(), Mock.Of<IReviewService>());
        TestHelper.SetupController(controller, userId: "admin", role: "ADMIN");

        var result = await controller.ChangeRole("u1", 0);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        redirect.ActionName.Should().Be("Index");
        controller.TempData["Message"].Should().Be("Role updated.");
    }

    [Fact]
    public async Task Delete_Redirects_With_Error_When_Self()
    {
        var userSvc = new Mock<IUserService>();
        userSvc.Setup(s => s.DeleteAsync("u1", "u1")).ReturnsAsync((false, "Cannot delete your own account."));

        var controller = new UsersController(userSvc.Object, Mock.Of<IOrderService>(),
            Mock.Of<ILibraryService>(), Mock.Of<IReviewService>());
        TestHelper.SetupController(controller, userId: "u1");

        var result = await controller.Delete("u1");

        Assert.IsType<RedirectToActionResult>(result);
        controller.TempData["Message"].Should().Be("Cannot delete your own account.");
        controller.TempData["IsError"].Should().Be(true);
    }

    [Fact]
    public async Task Details_Returns_View_When_User_Found()
    {
        var userSvc = new Mock<IUserService>();
        userSvc.Setup(s => s.GetByIdAsync("u1")).ReturnsAsync(
            new User { Id = "u1", Username = "Alice", Email = "a@t.com", PasswordHash = "h" });

        var controller = new UsersController(userSvc.Object, Mock.Of<IOrderService>(),
            Mock.Of<ILibraryService>(), Mock.Of<IReviewService>());
        TestHelper.SetupController(controller);

        var result = await controller.Details("u1");

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<UserDetailsViewModel>(view.Model);
        model.User.Username.Should().Be("Alice");
    }

    [Fact]
    public async Task Details_Redirects_When_User_NotFound()
    {
        var userSvc = new Mock<IUserService>();
        userSvc.Setup(s => s.GetByIdAsync("nonexistent")).ReturnsAsync((User?)null);

        var controller = new UsersController(userSvc.Object, Mock.Of<IOrderService>(),
            Mock.Of<ILibraryService>(), Mock.Of<IReviewService>());
        TestHelper.SetupController(controller);

        var result = await controller.Details("nonexistent");

        Assert.IsType<RedirectToActionResult>(result);
    }

    [Fact]
    public async Task Details_Redirects_When_Id_Empty()
    {
        var controller = new UsersController(Mock.Of<IUserService>(), Mock.Of<IOrderService>(),
            Mock.Of<ILibraryService>(), Mock.Of<IReviewService>());
        TestHelper.SetupController(controller);

        var result = await controller.Details("");

        Assert.IsType<RedirectToActionResult>(result);
    }
}
