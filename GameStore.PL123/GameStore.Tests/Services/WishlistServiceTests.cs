using FluentAssertions;

namespace GameStore.Tests.Services;

public class WishlistServiceTests
{
    private static GameStoreDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<GameStoreDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new GameStoreDbContext(options);
    }

    [Fact]
    public async Task AddToWishlistAsync_Adds_Game()
    {
        using var ctx = CreateContext("Wish_Add");
        ctx.Users.Add(new User { Id = "u1", Username = "Alice", Email = "a@t.com", PasswordHash = "h" });
        ctx.Games.Add(new Game { Id = "g1", Title = "Game", Price = 10m, ReleaseDate = DateTime.UtcNow });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new WishlistService(uow);

        var (success, error) = await service.AddToWishlistAsync("u1", "g1");

        success.Should().BeTrue();
        ctx.WishlistItems.Should().HaveCount(1);
    }

    [Fact]
    public async Task AddToWishlistAsync_Fails_If_Duplicate()
    {
        using var ctx = CreateContext("Wish_AddDup");
        ctx.Users.Add(new User { Id = "u1", Username = "Alice", Email = "a@t.com", PasswordHash = "h" });
        ctx.Games.Add(new Game { Id = "g1", Title = "Game", Price = 10m, ReleaseDate = DateTime.UtcNow });
        ctx.WishlistItems.Add(new WishlistItem { Id = "w1", UserId = "u1", GameId = "g1" });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new WishlistService(uow);

        var (success, error) = await service.AddToWishlistAsync("u1", "g1");

        success.Should().BeFalse();
        error.Should().Be("Game already in wishlist.");
    }

    [Fact]
    public async Task AddToWishlistAsync_Fails_If_Game_Not_Found()
    {
        using var ctx = CreateContext("Wish_AddNoGame");
        ctx.Users.Add(new User { Id = "u1", Username = "Alice", Email = "a@t.com", PasswordHash = "h" });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new WishlistService(uow);

        var (success, error) = await service.AddToWishlistAsync("u1", "nonexistent");

        success.Should().BeFalse();
        error.Should().Be("Game not found.");
    }

    [Fact]
    public async Task RemoveFromWishlistAsync_Removes_Item()
    {
        using var ctx = CreateContext("Wish_Remove");
        ctx.WishlistItems.Add(new WishlistItem { Id = "w1", UserId = "u1", GameId = "g1" });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new WishlistService(uow);

        var result = await service.RemoveFromWishlistAsync("w1", "u1");

        result.Should().BeTrue();
        ctx.WishlistItems.Should().BeEmpty();
    }

    [Fact]
    public async Task RemoveFromWishlistAsync_Returns_False_If_Not_Found()
    {
        using var ctx = CreateContext("Wish_RemoveNF");
        var uow = new UnitOfWork(ctx);
        var service = new WishlistService(uow);

        var result = await service.RemoveFromWishlistAsync("nonexistent", "u1");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetWishlistAsync_Returns_User_Wishlist()
    {
        using var ctx = CreateContext("Wish_Get");
        ctx.Users.Add(new User { Id = "u1", Username = "Alice", Email = "a@t.com", PasswordHash = "h" });
        ctx.Games.Add(new Game { Id = "g1", Title = "Game", Price = 10m, ReleaseDate = DateTime.UtcNow });
        ctx.WishlistItems.Add(new WishlistItem { Id = "w1", UserId = "u1", GameId = "g1" });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new WishlistService(uow);

        var items = await service.GetWishlistAsync("u1");

        items.Should().HaveCount(1);
        items[0].GameId.Should().Be("g1");
    }

    [Fact]
    public async Task IsInWishlistAsync_Returns_True_If_In_Wishlist()
    {
        using var ctx = CreateContext("Wish_IsIn");
        ctx.WishlistItems.Add(new WishlistItem { Id = "w1", UserId = "u1", GameId = "g1" });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new WishlistService(uow);

        var result = await service.IsInWishlistAsync("u1", "g1");

        result.Should().BeTrue();
    }
}
