using FluentAssertions;

namespace GameStore.Tests.Integration;

public class OrderFlowIntegrationTests
{
    private static GameStoreDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<GameStoreDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new GameStoreDbContext(options);
    }

    private static CartService CreateCartService(UnitOfWork uow)
    {
        var gameAccessMock = new Mock<IGameAccessService>();
        gameAccessMock.Setup(g => g.IsPreRelease(It.IsAny<Game>())).Returns(false);
        return new CartService(uow, gameAccessMock.Object);
    }

    [Fact]
    public async Task AddToCart_ThenRemove_FullFlow()
    {
        using var ctx = CreateContext("Integ_CartFlow");
        ctx.Users.Add(new User { Id = "u1", Username = "Buyer", Email = "b@test.com", PasswordHash = "h", Role = Role.CUSTOMER });
        var dev = new User { Id = "d1", Username = "Dev", Email = "d@test.com", PasswordHash = "h", Role = Role.DEVELOPER };
        ctx.Users.Add(dev);
        ctx.Developers.Add(new Developer { Id = "dev1", UserId = "d1", Name = "DevStudio", Slug = "devstudio", IsActive = true });
        ctx.Games.Add(new Game { Id = "g1", Title = "Game1", Price = 29.99m, DeveloperId = "dev1", ReleaseDate = DateTime.UtcNow.AddDays(-1) });
        ctx.Carts.Add(new Cart { Id = "c1", UserId = "u1" });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var cartService = CreateCartService(uow);

        await cartService.AddToCartAsync("u1", "g1");
        var items = await cartService.GetCartItemsAsync("u1");
        items.Should().HaveCount(1);
        items[0].Game!.Title.Should().Be("Game1");

        await cartService.RemoveFromCartAsync(items[0].Id, "u1");
        var itemsAfter = await cartService.GetCartItemsAsync("u1");
        itemsAfter.Should().BeEmpty();
    }

    [Fact]
    public async Task AddToCart_GameAlreadyOwned_Rejected()
    {
        using var ctx = CreateContext("Integ_CartOwned");
        ctx.Users.Add(new User { Id = "u1", Username = "Buyer", Email = "b@test.com", PasswordHash = "h", Role = Role.CUSTOMER });
        var dev = new User { Id = "d1", Username = "Dev", Email = "d@test.com", PasswordHash = "h", Role = Role.DEVELOPER };
        ctx.Users.Add(dev);
        ctx.Developers.Add(new Developer { Id = "dev1", UserId = "d1", Name = "DevStudio", Slug = "devstudio", IsActive = true });
        ctx.Games.Add(new Game { Id = "g1", Title = "Game1", Price = 29.99m, DeveloperId = "dev1", ReleaseDate = DateTime.UtcNow.AddDays(-1) });
        ctx.Carts.Add(new Cart { Id = "c1", UserId = "u1" });
        ctx.Libraries.Add(new Library { Id = "lib1", UserId = "u1" });
        ctx.LibraryGames.Add(new LibraryGame { LibraryId = "lib1", GameId = "g1" });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var cartService = CreateCartService(uow);

        var (success, error) = await cartService.AddToCartAsync("u1", "g1");

        success.Should().BeFalse();
        error.Should().Contain("own");
    }

    [Fact]
    public async Task AddToCart_DuplicateGame_Rejected()
    {
        using var ctx = CreateContext("Integ_CartDup");
        ctx.Users.Add(new User { Id = "u1", Username = "Buyer", Email = "b@test.com", PasswordHash = "h", Role = Role.CUSTOMER });
        var dev = new User { Id = "d1", Username = "Dev", Email = "d@test.com", PasswordHash = "h", Role = Role.DEVELOPER };
        ctx.Users.Add(dev);
        ctx.Developers.Add(new Developer { Id = "dev1", UserId = "d1", Name = "DevStudio", Slug = "devstudio", IsActive = true });
        ctx.Games.Add(new Game { Id = "g1", Title = "Game1", Price = 29.99m, DeveloperId = "dev1", ReleaseDate = DateTime.UtcNow.AddDays(-1) });
        ctx.Carts.Add(new Cart { Id = "c1", UserId = "u1" });
        ctx.CartItems.Add(new CartItem { Id = "ci1", CartId = "c1", GameId = "g1", Quantity = 1 });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var cartService = CreateCartService(uow);

        var (dupSuccess, dupError) = await cartService.AddToCartAsync("u1", "g1");

        dupSuccess.Should().BeFalse();
        dupError.Should().Contain("cart");
    }

    [Fact]
    public async Task WishlistAddRemove_FullFlow()
    {
        using var ctx = CreateContext("Integ_WishlistFlow");
        ctx.Users.Add(new User { Id = "u1", Username = "Fan", Email = "f@test.com", PasswordHash = "h", Role = Role.CUSTOMER });
        var dev = new User { Id = "d1", Username = "Dev", Email = "d@test.com", PasswordHash = "h", Role = Role.DEVELOPER };
        ctx.Users.Add(dev);
        ctx.Developers.Add(new Developer { Id = "dev1", UserId = "d1", Name = "DevStudio", Slug = "devstudio", IsActive = true });
        ctx.Games.Add(new Game { Id = "g1", Title = "Game1", Price = 29.99m, DeveloperId = "dev1", ReleaseDate = DateTime.UtcNow.AddDays(-1) });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var wishlistService = new WishlistService(uow);

        var (addSuccess, _) = await wishlistService.AddToWishlistAsync("u1", "g1");
        addSuccess.Should().BeTrue();
        (await wishlistService.IsInWishlistAsync("u1", "g1")).Should().BeTrue();

        var items = await wishlistService.GetWishlistAsync("u1");
        items.Should().HaveCount(1);

        await wishlistService.RemoveFromWishlistAsync(items[0].Id, "u1");
        (await wishlistService.IsInWishlistAsync("u1", "g1")).Should().BeFalse();
    }

    [Fact]
    public async Task LibraryAddThenCheck_FullFlow()
    {
        using var ctx = CreateContext("Integ_LibFlow");
        ctx.Users.Add(new User { Id = "u1", Username = "Player", Email = "p@test.com", PasswordHash = "h", Role = Role.CUSTOMER });
        var dev = new User { Id = "d1", Username = "Dev", Email = "d@test.com", PasswordHash = "h", Role = Role.DEVELOPER };
        ctx.Users.Add(dev);
        ctx.Developers.Add(new Developer { Id = "dev1", UserId = "d1", Name = "DevStudio", Slug = "devstudio", IsActive = true });
        ctx.Games.Add(new Game { Id = "g1", Title = "Game1", Price = 29.99m, DeveloperId = "dev1" });
        ctx.Libraries.Add(new Library { Id = "lib1", UserId = "u1" });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var libService = new LibraryService(uow);

        (await libService.HasGame("u1", "g1")).Should().BeFalse();

        await libService.AddGameToLibraryAsync("u1", "g1");

        (await libService.HasGame("u1", "g1")).Should().BeTrue();
    }

    [Fact]
    public async Task ReviewCreateDelete_FullFlow()
    {
        using var ctx = CreateContext("Integ_ReviewFlow");
        ctx.Users.Add(new User { Id = "u1", Username = "Critic", Email = "c@test.com", PasswordHash = "h", Role = Role.CUSTOMER });
        var dev = new User { Id = "d1", Username = "Dev", Email = "d@test.com", PasswordHash = "h", Role = Role.DEVELOPER };
        ctx.Users.Add(dev);
        ctx.Developers.Add(new Developer { Id = "dev1", UserId = "d1", Name = "DevStudio", Slug = "devstudio", IsActive = true });
        ctx.Games.Add(new Game { Id = "g1", Title = "Game1", Price = 29.99m, DeveloperId = "dev1" });
        ctx.Libraries.Add(new Library { Id = "lib1", UserId = "u1" });
        ctx.LibraryGames.Add(new LibraryGame { LibraryId = "lib1", GameId = "g1" });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var libService = new LibraryService(uow);
        var reviewService = new ReviewService(uow, libService);

        var (createSuccess, _) = await reviewService.CreateAsync("u1", "g1", 5, "Amazing game!");
        createSuccess.Should().BeTrue();
        var reviews = await reviewService.GetByGameAsync("g1");
        reviews.Should().HaveCount(1);
        reviews[0].Rating.Should().Be(5);

        await reviewService.DeleteAsync(reviews[0].Id);
        var final = await reviewService.GetByGameAsync("g1");
        final.Should().BeEmpty();
    }

    [Fact]
    public async Task ChatMessage_FullFlow()
    {
        using var ctx = CreateContext("Integ_ChatFlow");
        ctx.Users.AddRange(
            new User { Id = "u1", Username = "Alice", Email = "a@test.com", PasswordHash = "h", Role = Role.CUSTOMER },
            new User { Id = "u2", Username = "Bob", Email = "b@test.com", PasswordHash = "h", Role = Role.CUSTOMER }
        );
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var chatService = new ChatService(uow);

        var msg = await chatService.SendMessageAsync("u1", "u2", "Hello Bob!");
        msg.Content.Should().Be("Hello Bob!");
        msg.SenderId.Should().Be("u1");

        var unread = await chatService.GetUnreadCountAsync("u2");
        unread.Should().Be(1);

        await chatService.MarkAsReadAsync("u1", "u2");
        var unreadAfter = await chatService.GetUnreadCountAsync("u2");
        unreadAfter.Should().Be(0);
    }
}
