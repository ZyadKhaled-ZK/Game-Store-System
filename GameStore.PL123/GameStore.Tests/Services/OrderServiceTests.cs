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
    public async Task PlaceOrderAsync_Creates_Order_And_Library()
    {
        using var ctx = CreateContext("Order_Place");
        await SeedCartWithItems(ctx, "u1", "g1");
        var uow = new UnitOfWork(ctx);
        var service = new OrderService(uow);

        var (success, msg) = await service.PlaceOrderAsync("u1");

        success.Should().BeTrue();
        ctx.Orders.Should().HaveCount(1);
        ctx.OrderItems.Should().HaveCount(1);
        ctx.LibraryGames.Should().HaveCount(1);
        ctx.CartItems.Should().BeEmpty();
    }

    [Fact]
    public async Task PlaceOrderAsync_Fails_If_Cart_Empty()
    {
        using var ctx = CreateContext("Order_PlaceEmpty");
        ctx.Users.Add(new User { Id = "u1", Username = "User", Email = "u@t.com", PasswordHash = "h" });
        ctx.Carts.Add(new Cart { Id = "cart1", UserId = "u1" });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new OrderService(uow);

        var (success, msg) = await service.PlaceOrderAsync("u1");

        success.Should().BeFalse();
        msg.Should().Be("Your cart is empty.");
    }

    [Fact]
    public async Task GetOrderByIdAsync_Returns_Order()
    {
        using var ctx = CreateContext("Order_GetById");
        ctx.Users.Add(new User { Id = "u1", Username = "U", Email = "u@t.com", PasswordHash = "h" });
        ctx.Orders.Add(new Order { Id = "o1", UserId = "u1", TotalPrice = 10m });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new OrderService(uow);

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
        var service = new OrderService(uow);

        var orders = await service.GetOrdersByUserAsync("u1");

        orders.Should().HaveCount(2);
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
}
