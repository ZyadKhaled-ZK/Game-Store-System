using FluentAssertions;
using GameStore.BLL.Models;
using GameStore.PL.Areas.Admin.Controllers;
using GameStore.PL.Models.Admin;
using Moq;

namespace GameStore.Tests.Controllers.Admin;

public class ReviewsControllerTests
{
    [Fact]
    public async Task Index_Returns_View_With_Paged_Reviews()
    {
        var svc = new Mock<IReviewService>();
        svc.Setup(s => s.GetAllPagedAsync(1, 50)).ReturnsAsync(new PagedResult<Review>
        {
            Items = [new Review { Id = "r1", GameId = "g1", UserId = "u1", Rating = 4 }],
            Page = 1,
            PageSize = 50,
            TotalCount = 1
        });

        var controller = new ReviewsController(svc.Object);
        TestHelper.SetupController(controller);

        var result = await controller.Index(null);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ManageReviewsViewModel>(view.Model);
        model.Reviews.Should().HaveCount(1);
        model.CurrentPage.Should().Be(1);
        model.TotalPages.Should().Be(1);
    }

    [Fact]
    public async Task Index_Reads_TempData()
    {
        var svc = new Mock<IReviewService>();
        svc.Setup(s => s.GetAllPagedAsync(1, 50)).ReturnsAsync(new PagedResult<Review>
        {
            Items = new List<Review>(), Page = 1, PageSize = 50, TotalCount = 0
        });

        var controller = new ReviewsController(svc.Object);
        TestHelper.SetupController(controller);
        controller.TempData["Message"] = "Test msg";
        controller.TempData["IsError"] = true;

        var result = await controller.Index(null);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ManageReviewsViewModel>(view.Model);
        model.Message.Should().Be("Test msg");
        model.IsError.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_Redirects_With_Success()
    {
        var svc = new Mock<IReviewService>();
        svc.Setup(s => s.DeleteAsync("r1")).ReturnsAsync(true);

        var controller = new ReviewsController(svc.Object);
        TestHelper.SetupController(controller);

        var result = await controller.Delete("r1");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        redirect.ActionName.Should().Be("Index");
    }
}
