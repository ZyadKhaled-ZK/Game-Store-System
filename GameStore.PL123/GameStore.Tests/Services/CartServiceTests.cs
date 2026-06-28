using FluentAssertions;

namespace GameStore.Tests.Services;

public class CartServiceTests
{
    private static GameStoreDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<GameStoreDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new GameStoreDbContext(options);
    }

    private static async Task SeedBasicData(GameStoreDbContext ctx)
    {
        ctx.Users.Add(new User { Id = "u1", Username = "Alice", Email = "a@t.com", PasswordHash = "h" });
        ctx.Games.AddRange(
            new Game { Id = "g1", Title = "Game One", Price = 9.99m },
            new Game { Id = "g2", Title = "Game Two", Price = 19.99m },
            new Game { Id = "g3", Title = "Game Three", Price = 0m }
        );
        await ctx.SaveChangesAsync();
    }

    [Fact]
    public async Task AddToCartAsync_Adds_Game_To_Cart()
    {
        using var ctx = CreateContext("Cart_Add");
        await SeedBasicData(ctx);
        var uow = new UnitOfWork(ctx);
        var service = new CartService(uow);

        var (success, error) = await service.AddToCartAsync("u1", "g1");

        success.Should().BeTrue();
        var cart = ctx.Carts.Include(c => c.CartItems).First(c => c.UserId == "u1");
        cart.CartItems.Should().HaveCount(1);
        cart.CartItems.First().GameId.Should().Be("g1");
    }

    [Fact]
    public async Task AddToCartAsync_Creates_Cart_If_Not_Exists()
    {
        using var ctx = CreateContext("Cart_AddNoCart");
        await SeedBasicData(ctx);
        var uow = new UnitOfWork(ctx);
        var service = new CartService(uow);

        await service.AddToCartAsync("u1", "g1");

        ctx.Carts.Should().HaveCount(1);
        ctx.CartItems.Should().HaveCount(1);
    }

    [Fact]
    public async Task AddToCartAsync_Fails_If_Duplicate_Game()
    {
        using var ctx = CreateContext("Cart_AddDup");
        await SeedBasicData(ctx);
        var uow = new UnitOfWork(ctx);
        var service = new CartService(uow);

        await service.AddToCartAsync("u1", "g1");
        var (success, error) = await service.AddToCartAsync("u1", "g1");

        success.Should().BeFalse();
        error.Should().Be("Game already in cart.");
    }

    [Fact]
    public async Task AddToCartAsync_Fails_If_Game_Not_Found()
    {
        using var ctx = CreateContext("Cart_AddNoGame");
        ctx.Users.Add(new User { Id = "u1", Username = "Alice", Email = "a@t.com", PasswordHash = "h" });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new CartService(uow);

        var (success, error) = await service.AddToCartAsync("u1", "nonexistent");

        success.Should().BeFalse();
        error.Should().Be("Game not found.");
    }

    [Fact]
    public async Task GetCartItemsAsync_Returns_Cart_Items()
    {
        using var ctx = CreateContext("Cart_GetItems");
        await SeedBasicData(ctx);
        var uow = new UnitOfWork(ctx);
        var service = new CartService(uow);

        await service.AddToCartAsync("u1", "g1");
        await service.AddToCartAsync("u1", "g2");

        var items = await service.GetCartItemsAsync("u1");

        items.Should().HaveCount(2);
        items.Select(i => i.GameId).Should().Contain(new[] { "g1", "g2" });
    }

    [Fact]
    public async Task GetCartItemsAsync_Returns_Empty_If_No_Cart()
    {
        using var ctx = CreateContext("Cart_GetItemsEmpty");
        await SeedBasicData(ctx);
        var uow = new UnitOfWork(ctx);
        var service = new CartService(uow);

        var items = await service.GetCartItemsAsync("u1");

        items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCartCountAsync_Returns_Correct_Count()
    {
        using var ctx = CreateContext("Cart_Count");
        await SeedBasicData(ctx);
        var uow = new UnitOfWork(ctx);
        var service = new CartService(uow);

        await service.AddToCartAsync("u1", "g1");
        await service.AddToCartAsync("u1", "g2");

        var count = await service.GetCartCountAsync("u1");

        count.Should().Be(2);
    }

    [Fact]
    public async Task RemoveFromCartAsync_Removes_Item()
    {
        using var ctx = CreateContext("Cart_Remove");
        await SeedBasicData(ctx);
        var uow = new UnitOfWork(ctx);
        var service = new CartService(uow);

        await service.AddToCartAsync("u1", "g1");
        var itemId = ctx.CartItems.First().Id;

        var removed = await service.RemoveFromCartAsync(itemId, "u1");

        removed.Should().BeTrue();
        ctx.CartItems.Should().BeEmpty();
    }

    [Fact]
    public async Task RemoveFromCartAsync_Returns_False_If_Not_Found()
    {
        using var ctx = CreateContext("Cart_RemoveNotFound");
        var uow = new UnitOfWork(ctx);
        var service = new CartService(uow);

        var removed = await service.RemoveFromCartAsync("nonexistent", "u1");

        removed.Should().BeFalse();
    }

    [Fact]
    public async Task ClearCartAsync_Removes_All_Items()
    {
        using var ctx = CreateContext("Cart_Clear");
        await SeedBasicData(ctx);
        var uow = new UnitOfWork(ctx);
        var service = new CartService(uow);

        await service.AddToCartAsync("u1", "g1");
        await service.AddToCartAsync("u1", "g2");
        await service.AddToCartAsync("u1", "g3");

        await service.ClearCartAsync("u1");

        ctx.CartItems.Should().BeEmpty();
    }
}
