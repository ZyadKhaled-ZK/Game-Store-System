using FluentAssertions;
using GameStore.BLL.Models;
using GameStore.PL.Areas.Admin.Controllers;
using GameStore.PL.Models.Admin;
using Moq;

namespace GameStore.Tests.Controllers.Admin;

public class DashboardControllerTests
{
    [Fact]
    public async Task Index_Returns_View_With_Model()
    {
        var userSvc = new Mock<IUserService>();
        var gameSvc = new Mock<IGameService>();
        var orderSvc = new Mock<IOrderService>();
        var analyticsSvc = new Mock<IOrderAnalyticsService>();

        userSvc.Setup(s => s.GetTotalUsersAsync()).ReturnsAsync(10);
        gameSvc.Setup(s => s.GetTotalGamesAsync()).ReturnsAsync(20);
        gameSvc.Setup(s => s.GetFreeGamesCountAsync()).ReturnsAsync(0);
        analyticsSvc.Setup(s => s.GetTotalOrdersAsync()).ReturnsAsync(30);
        analyticsSvc.Setup(s => s.GetTotalRevenueAsync()).ReturnsAsync(100m);
        analyticsSvc.Setup(s => s.GetAverageOrderValueAsync()).ReturnsAsync(3.33m);
        analyticsSvc.Setup(s => s.GetRevenueByMonthAsync(12)).ReturnsAsync(new List<MonthlyRevenue>());
        analyticsSvc.Setup(s => s.GetOrdersByDayAsync(30)).ReturnsAsync(new List<DailyOrderCount>());
        analyticsSvc.Setup(s => s.GetTopSellingGamesAsync(5)).ReturnsAsync(new List<TopGameSale>());
        analyticsSvc.Setup(s => s.GetRevenueByCategoryAsync()).ReturnsAsync(new List<CategoryRevenue>());
        userSvc.Setup(s => s.GetUsersByRoleAsync()).ReturnsAsync(new List<UsersByRole>());
        gameSvc.Setup(s => s.GetGamesByCategoryAsync()).ReturnsAsync(new List<GamesByCategory>());
        userSvc.Setup(s => s.GetUsersByMonthAsync(12)).ReturnsAsync(new List<UsersByMonth>());
        analyticsSvc.Setup(s => s.GetOrderCountSinceAsync(It.IsAny<DateTime>())).ReturnsAsync(5);
        analyticsSvc.Setup(s => s.GetRevenueSinceAsync(It.IsAny<DateTime>())).ReturnsAsync(50m);
        orderSvc.Setup(s => s.GetRecentWithDetailsAsync(5)).ReturnsAsync(new List<Order>());

        var controller = new DashboardController(userSvc.Object, gameSvc.Object, orderSvc.Object, analyticsSvc.Object);
        TestHelper.SetupController(controller);

        var result = await controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<DashboardViewModel>(view.Model);
        model.TotalUsers.Should().Be(10);
        model.TotalGames.Should().Be(20);
        model.TotalOrders.Should().Be(30);
        model.TotalRevenue.Should().Be(100m);
        model.TodayOrdersCount.Should().Be(5);
        model.TodayRevenue.Should().Be(50m);
    }
}
