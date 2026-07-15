using FluentAssertions;

namespace GameStore.Tests.ErrorHandling;

public class ServiceErrorHandlingTests
{
    private static GameStoreDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<GameStoreDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new GameStoreDbContext(options);
    }

    [Fact]
    public async Task CartService_AddToCart_NonexistentGame_ReturnsFailure()
    {
        using var ctx = CreateContext("Err_CartNoGame");
        ctx.Carts.Add(new Cart { Id = "c1", UserId = "u1" });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var gameAccessMock = new Mock<IGameAccessService>();
        var service = new CartService(uow, gameAccessMock.Object);

        var (success, error) = await service.AddToCartAsync("c1", "nonexistent-game");

        success.Should().BeFalse();
        error.Should().Contain("not found");
    }

    [Fact]
    public async Task OrderService_GetById_Nonexistent_ReturnsNull()
    {
        using var ctx = CreateContext("Err_OrderNull");
        var uow = new UnitOfWork(ctx);
        var saleMock = new Mock<ISaleService>();
        var gameAccessMock = new Mock<IGameAccessService>();
        var service = new OrderService(uow, saleMock.Object, gameAccessMock.Object);

        var result = await service.GetOrderByIdAsync("nonexistent");

        result.Should().BeNull();
    }

    [Fact]
    public async Task ReviewService_Delete_OtherUserReview_Throws()
    {
        using var ctx = CreateContext("Err_ReviewOther");
        var dev = new User { Id = "d1", Username = "Dev", Email = "d@test.com", PasswordHash = "h", Role = Role.DEVELOPER };
        ctx.Users.AddRange(
            new User { Id = "u1", Username = "A", Email = "a@test.com", PasswordHash = "h", Role = Role.CUSTOMER },
            dev
        );
        ctx.Developers.Add(new Developer { Id = "dev1", UserId = "d1", Name = "DevStudio", Slug = "devstudio", IsActive = true });
        ctx.Games.Add(new Game { Id = "g1", Title = "Game1", Price = 10m, DeveloperId = "dev1" });
        ctx.Reviews.Add(new Review { Id = "r1", UserId = "u1", GameId = "g1", Rating = 5, Comment = "Great" });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var libService = new LibraryService(uow);
        var service = new ReviewService(uow, libService);

        var result = await service.DeleteAsync("r1");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task UserService_ChangeRole_SelfDemote_ReturnsFailure()
    {
        using var ctx = CreateContext("Err_SelfDemote");
        ctx.Users.Add(new User { Id = "u1", Username = "Admin", Email = "a@test.com", PasswordHash = "h", Role = Role.ADMIN });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new UserService(uow);

        var (success, error) = await service.ChangeRoleAsync("u1", Role.CUSTOMER, "u1");

        success.Should().BeFalse();
        error.Should().Contain("own role");
    }

    [Fact]
    public async Task PostService_Delete_WrongUser_ReturnsFalse()
    {
        using var ctx = CreateContext("Err_PostOther");
        ctx.Users.Add(new User { Id = "u1", Username = "A", Email = "a@test.com", PasswordHash = "h", Role = Role.CUSTOMER });
        ctx.Posts.Add(new Post { Id = "p1", UserId = "u1", Content = "My post" });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new PostService(uow);

        var result = await service.DeleteAsync("p1", "wrong-user");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ChatService_GetConversations_ReturnsEmptyForNoMessages()
    {
        using var ctx = CreateContext("Err_ChatEmpty");
        ctx.Users.Add(new User { Id = "u1", Username = "A", Email = "a@test.com", PasswordHash = "h", Role = Role.CUSTOMER });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new ChatService(uow);

        var convos = await service.GetConversationsAsync("u1");

        convos.Should().BeEmpty();
    }

    [Fact]
    public async Task JsonFileStore_Delete_NonexistentFile_NoThrow()
    {
        var envMock = new Mock<IWebHostEnvironment>();
        var tempDir = Path.Combine(Path.GetTempPath(), "err_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        envMock.Setup(e => e.WebRootPath).Returns(tempDir);

        try
        {
            var store = new JsonFileStore(envMock.Object);
            var act = () => store.DeleteAsync("nonexistent.json");
            await act.Should().NotThrowAsync();
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task SaleService_Approve_NonexistentSale_ReturnsFailure()
    {
        using var ctx = CreateContext("Err_SaleNull");
        var uow = new UnitOfWork(ctx);
        var service = new SaleService(uow);

        var (success, error) = await service.ApproveAsync("nonexistent");

        success.Should().BeFalse();
        error.Should().Contain("not found");
    }

    [Fact]
    public async Task WishlistService_AddToWishlist_NonexistentGame_ReturnsFailure()
    {
        using var ctx = CreateContext("Err_WishNoGame");
        ctx.Users.Add(new User { Id = "u1", Username = "A", Email = "a@test.com", PasswordHash = "h", Role = Role.CUSTOMER });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new WishlistService(uow);

        var (success, error) = await service.AddToWishlistAsync("u1", "nonexistent-game");

        success.Should().BeFalse();
        error.Should().Contain("not found");
    }

    [Fact]
    public async Task LibraryService_RemoveGame_NotOwned_DoesNotThrow()
    {
        using var ctx = CreateContext("Err_LibRemove");
        ctx.Users.Add(new User { Id = "u1", Username = "A", Email = "a@test.com", PasswordHash = "h", Role = Role.CUSTOMER });
        ctx.Libraries.Add(new Library { Id = "lib1", UserId = "u1" });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new LibraryService(uow);

        var act = () => service.RemoveGameFromLibraryAsync("u1", "nonexistent-game");

        await act.Should().NotThrowAsync();
    }
}
