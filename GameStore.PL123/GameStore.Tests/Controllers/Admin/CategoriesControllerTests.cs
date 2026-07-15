using FluentAssertions;
using GameStore.PL.Areas.Admin.Controllers;
using GameStore.PL.Models.Admin;
using Moq;

namespace GameStore.Tests.Controllers.Admin;

public class CategoriesControllerTests
{
    [Fact]
    public async Task Index_Returns_View_With_All_Categories()
    {
        var svc = new Mock<ICategoryService>();
        svc.Setup(s => s.GetAllWithGameCountAsync()).ReturnsAsync([
            new Category { Id = "c1", Name = "Action" },
            new Category { Id = "c2", Name = "RPG" }
        ]);

        var controller = new CategoriesController(svc.Object);
        TestHelper.SetupController(controller);

        var result = await controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ManageCategoriesViewModel>(view.Model);
        model.Categories.Should().HaveCount(2);
    }

    [Fact]
    public async Task Create_Redirects_With_Success()
    {
        var svc = new Mock<ICategoryService>();
        svc.Setup(s => s.CreateAsync("Action")).ReturnsAsync((true, ""));

        var controller = new CategoriesController(svc.Object);
        TestHelper.SetupController(controller);

        var result = await controller.Create("Action");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        redirect.ActionName.Should().Be("Index");
        controller.TempData["Message"]!.ToString().Should().Contain("added");
    }

    [Fact]
    public async Task Create_Redirects_With_Error_When_Invalid()
    {
        var controller = new CategoriesController(Mock.Of<ICategoryService>());
        TestHelper.SetupController(controller);
        controller.ModelState.AddModelError("Name", "Required");

        var result = await controller.Create("");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        controller.TempData["IsError"].Should().Be(true);
    }

    [Fact]
    public async Task Edit_Redirects_With_Success()
    {
        var svc = new Mock<ICategoryService>();
        svc.Setup(s => s.UpdateAsync("c1", "New Name")).ReturnsAsync(true);

        var controller = new CategoriesController(svc.Object);
        TestHelper.SetupController(controller);

        var result = await controller.Edit("c1", "New Name");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        controller.TempData["Message"]!.ToString().Should().Contain("renamed");
    }

    [Fact]
    public async Task Delete_Redirects_With_Success()
    {
        var svc = new Mock<ICategoryService>();
        svc.Setup(s => s.DeleteAsync("c1")).ReturnsAsync(true);

        var controller = new CategoriesController(svc.Object);
        TestHelper.SetupController(controller);

        var result = await controller.Delete("c1");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        controller.TempData["Message"]!.ToString().Should().Contain("deleted");
    }

    [Fact]
    public async Task Delete_Redirects_With_Error_When_Has_Games()
    {
        var svc = new Mock<ICategoryService>();
        svc.Setup(s => s.DeleteAsync("c1")).ReturnsAsync(false);

        var controller = new CategoriesController(svc.Object);
        TestHelper.SetupController(controller);

        var result = await controller.Delete("c1");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        controller.TempData["Message"]!.ToString().Should().Contain("Cannot delete");
        controller.TempData["IsError"].Should().Be(true);
    }
}
