using GameStore.BLL.Models;
using Moq;
using FluentAssertions;

namespace GameStore.Tests.Services;

public class OrderServiceTests
{
    private static GameStoreDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<GameStoreDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new GameStoreDbContext(options);
    }

    private static async Task SeedCartWithItems(GameStoreDbContext ctx, string userId, string gameId)
    {
        ctx.Users.Add(new User { Id = userId, Username = "User", Email = "u@t.com", PasswordHash = "h" });
        ctx.Games.Add(new Game { Id = gameId, Title = "Game", Price = 19.99m, ReleaseDate = DateTime.UtcNow });
        ctx.Carts.Add(new Cart { Id = "cart1", UserId = userId });
        ctx.CartItems.Add(new CartItem { Id = "ci1", CartId = "cart1", GameId = gameId });
        await ctx.SaveChangesAsync();
    }

    [Fact]
    public async Task GetOrderByIdAsync_Returns_Order()
    {
        using var ctx = CreateContext("Order_GetById");
        ctx.Users.Add(new User { Id = "u1", Username = "U", Email = "u@t.com", PasswordHash = "h" });
        ctx.Orders.Add(new Order { Id = "o1", UserId = "u1", TotalPrice = 10m });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new OrderService(uow, Mock.Of<ISaleService>(), Mock.Of<IGameAccessService>());

        var order = await service.GetOrderByIdAsync("o1");

        order.Should().NotBeNull();
        order!.TotalPrice.Should().Be(10m);
    }

    [Fact]
    public async Task GetOrdersByUserAsync_Returns_User_Orders()
    {
        using var ctx = CreateContext("Order_ByUser");
        ctx.Users.Add(new User { Id = "u1", Username = "U", Email = "u@t.com", PasswordHash = "h" });
        ctx.Orders.AddRange(
            new Order { Id = "o1", UserId = "u1", TotalPrice = 10m },
            new Order { Id = "o2", UserId = "u1", TotalPrice = 20m }
        );
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new OrderService(uow, Mock.Of<ISaleService>(), Mock.Of<IGameAccessService>());

        var orders = await service.GetOrdersByUserAsync("u1");

        orders.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllWithDetailsAsync_Returns_All_Orders()
    {
        using var ctx = CreateContext("Order_GetAll");
        ctx.Users.Add(new User { Id = "u1", Username = "U", Email = "u@t.com", PasswordHash = "h" });
        ctx.Orders.AddRange(
            new Order { Id = "o1", UserId = "u1", TotalPrice = 10m },
            new Order { Id = "o2", UserId = "u1", TotalPrice = 20m }
        );
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new OrderService(uow, Mock.Of<ISaleService>(), Mock.Of<IGameAccessService>());

        var orders = await service.GetAllWithDetailsAsync();

        orders.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetRecentWithDetailsAsync_Returns_Recent_Orders()
    {
        using var ctx = CreateContext("Order_Recent");
        ctx.Users.Add(new User { Id = "u1", Username = "U", Email = "u@t.com", PasswordHash = "h" });
        ctx.Orders.AddRange(
            new Order { Id = "o1", UserId = "u1", TotalPrice = 10m },
            new Order { Id = "o2", UserId = "u1", TotalPrice = 20m }
        );
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new OrderService(uow, Mock.Of<ISaleService>(), Mock.Of<IGameAccessService>());

        var orders = await service.GetRecentWithDetailsAsync(1);

        orders.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByStripeSessionIdAsync_Returns_Order()
    {
        using var ctx = CreateContext("Order_ByStripe");
        ctx.Users.Add(new User { Id = "u1", Username = "U", Email = "u@t.com", PasswordHash = "h" });
        ctx.Orders.Add(new Order { Id = "o1", UserId = "u1", TotalPrice = 10m, StripeSessionId = "sess_123" });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new OrderService(uow, Mock.Of<ISaleService>(), Mock.Of<IGameAccessService>());

        var order = await service.GetByStripeSessionIdAsync("sess_123");

        order.Should().NotBeNull();
        order!.Id.Should().Be("o1");
    }

    [Fact]
    public async Task GetByStripeSessionIdAsync_Returns_Null_If_Not_Found()
    {
        using var ctx = CreateContext("Order_ByStripeNF");
        var uow = new UnitOfWork(ctx);
        var service = new OrderService(uow, Mock.Of<ISaleService>(), Mock.Of<IGameAccessService>());

        var order = await service.GetByStripeSessionIdAsync("nonexistent");

        order.Should().BeNull();
    }
}

public class OrderAnalyticsServiceTests
{
    private static GameStoreDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<GameStoreDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new GameStoreDbContext(options);
    }

    [Fact]
    public async Task GetTotalOrdersAsync_Returns_Count()
    {
        using var ctx = CreateContext("OA_TotalOrders");
        ctx.Orders.AddRange(
            new Order { Id = "o1", UserId = "u1", TotalPrice = 10m },
            new Order { Id = "o2", UserId = "u1", TotalPrice = 20m }
        );
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new OrderAnalyticsService(uow);

        var count = await service.GetTotalOrdersAsync();

        count.Should().Be(2);
    }

    [Fact]
    public async Task GetTotalRevenueAsync_Returns_Sum()
    {
        using var ctx = CreateContext("OA_Revenue");
        ctx.Orders.AddRange(
            new Order { Id = "o1", UserId = "u1", TotalPrice = 10m },
            new Order { Id = "o2", UserId = "u1", TotalPrice = 20m }
        );
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new OrderAnalyticsService(uow);

        var revenue = await service.GetTotalRevenueAsync();

        revenue.Should().Be(30m);
    }

    [Fact]
    public async Task GetAverageOrderValueAsync_Returns_Average()
    {
        using var ctx = CreateContext("OA_Avg");
        ctx.Orders.AddRange(
            new Order { Id = "o1", UserId = "u1", TotalPrice = 10m },
            new Order { Id = "o2", UserId = "u1", TotalPrice = 30m }
        );
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new OrderAnalyticsService(uow);

        var avg = await service.GetAverageOrderValueAsync();

        avg.Should().Be(20m);
    }

    [Fact]
    public async Task GetAverageOrderValueAsync_Returns_Zero_If_No_Orders()
    {
        using var ctx = CreateContext("OA_AvgZero");
        var uow = new UnitOfWork(ctx);
        var service = new OrderAnalyticsService(uow);

        var avg = await service.GetAverageOrderValueAsync();

        avg.Should().Be(0);
    }

    [Fact]
    public async Task GetRevenueByMonthAsync_Returns_Monthly_Revenue()
    {
        using var ctx = CreateContext("OA_RevenueMonth");
        ctx.Orders.Add(new Order { Id = "o1", UserId = "u1", TotalPrice = 100m, CreatedAt = DateTime.UtcNow });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new OrderAnalyticsService(uow);

        var data = await service.GetRevenueByMonthAsync(1);

        data.Should().HaveCount(1);
        data[0].Revenue.Should().Be(100m);
    }

    [Fact]
    public async Task GetOrdersByDayAsync_Returns_Daily_Counts()
    {
        using var ctx = CreateContext("OA_OrdersDay");
        ctx.Orders.Add(new Order { Id = "o1", UserId = "u1", TotalPrice = 10m, CreatedAt = DateTime.UtcNow });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new OrderAnalyticsService(uow);

        var data = await service.GetOrdersByDayAsync(1);

        data.Should().HaveCount(1);
        data[0].Count.Should().Be(1);
    }

    [Fact]
    public async Task GetTopSellingGamesAsync_Returns_Top_Games()
    {
        using var ctx = CreateContext("OA_TopGames");
        ctx.Games.Add(new Game { Id = "g1", Title = "Top Game", Price = 10m, ReleaseDate = DateTime.UtcNow });
        ctx.OrderItems.AddRange(
            new OrderItem { Id = "oi1", OrderId = "o1", GameId = "g1", PriceAtPurchase = 10m },
            new OrderItem { Id = "oi2", OrderId = "o2", GameId = "g1", PriceAtPurchase = 10m }
        );
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new OrderAnalyticsService(uow);

        var data = await service.GetTopSellingGamesAsync(5);

        data.Should().HaveCount(1);
        data[0].Title.Should().Be("Top Game");
        data[0].Count.Should().Be(2);
    }

    [Fact]
    public async Task GetRevenueByCategoryAsync_Returns_Category_Revenue()
    {
        using var ctx = CreateContext("OA_RevenueCat");
        ctx.Categories.Add(new Category { Id = "c1", Name = "Action" });
        ctx.Games.Add(new Game { Id = "g1", Title = "Game", Price = 10m, ReleaseDate = DateTime.UtcNow });
        ctx.GameCategories.Add(new GameCategory { GameId = "g1", CategoryId = "c1" });
        ctx.OrderItems.Add(new OrderItem { Id = "oi1", OrderId = "o1", GameId = "g1", PriceAtPurchase = 10m });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new OrderAnalyticsService(uow);

        var data = await service.GetRevenueByCategoryAsync();

        data.Should().HaveCount(1);
        data[0].Category.Should().Be("Action");
        data[0].Revenue.Should().Be(10m);
    }

    [Fact]
    public async Task GetOrderCountSinceAsync_Returns_Count_Since_Date()
    {
        using var ctx = CreateContext("OA_CountSince");
        ctx.Orders.Add(new Order { Id = "o1", UserId = "u1", TotalPrice = 10m, CreatedAt = DateTime.UtcNow });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new OrderAnalyticsService(uow);

        var count = await service.GetOrderCountSinceAsync(DateTime.UtcNow.AddDays(-1));

        count.Should().Be(1);
    }

    [Fact]
    public async Task GetRevenueSinceAsync_Returns_Revenue_Since_Date()
    {
        using var ctx = CreateContext("OA_RevenueSince");
        ctx.Orders.Add(new Order { Id = "o1", UserId = "u1", TotalPrice = 50m, CreatedAt = DateTime.UtcNow });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new OrderAnalyticsService(uow);

        var revenue = await service.GetRevenueSinceAsync(DateTime.UtcNow.AddDays(-1));

        revenue.Should().Be(50m);
    }
}
