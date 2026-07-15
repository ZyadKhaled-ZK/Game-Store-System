using FluentAssertions;

namespace GameStore.Tests.Services;

public class DeveloperServiceTests
{
    private static GameStoreDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<GameStoreDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new GameStoreDbContext(options);
    }

    [Fact]
    public async Task CreateOrUpdateProfileAsync_Creates_New_Profile()
    {
        using var ctx = CreateContext("Dev_Create");
        ctx.Users.Add(new User { Id = "u1", Username = "Alice", Email = "a@t.com", PasswordHash = "h" });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new DeveloperService(uow);

        var (success, error) = await service.CreateOrUpdateProfileAsync("u1", "My Studio", "my-studio", "desc", null, null, null);

        success.Should().BeTrue();
        ctx.Developers.Should().HaveCount(1);
        ctx.Developers.Single().Name.Should().Be("My Studio");
    }

    [Fact]
    public async Task CreateOrUpdateProfileAsync_Updates_Existing_Profile()
    {
        using var ctx = CreateContext("Dev_Update");
        ctx.Users.Add(new User { Id = "u1", Username = "Alice", Email = "a@t.com", PasswordHash = "h" });
        ctx.Developers.Add(new Developer { Id = "d1", Name = "Old Studio", UserId = "u1", Slug = "old" });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new DeveloperService(uow);

        var (success, error) = await service.CreateOrUpdateProfileAsync("u1", "New Studio", "new", null, null, null, null);

        success.Should().BeTrue();
        ctx.Developers.Single().Name.Should().Be("New Studio");
    }

    [Fact]
    public async Task GetByUserIdAsync_Returns_Developer()
    {
        using var ctx = CreateContext("Dev_ByUserId");
        ctx.Developers.Add(new Developer { Id = "d1", Name = "Studio", UserId = "u1", Slug = "studio" });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new DeveloperService(uow);

        var dev = await service.GetByUserIdAsync("u1");

        dev.Should().NotBeNull();
        dev!.Name.Should().Be("Studio");
    }

    [Fact]
    public async Task GetByIdAsync_Returns_Developer()
    {
        using var ctx = CreateContext("Dev_ById");
        ctx.Developers.Add(new Developer { Id = "d1", Name = "Studio", UserId = "u1", Slug = "studio" });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new DeveloperService(uow);

        var dev = await service.GetByIdAsync("d1");

        dev.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAllAsync_Returns_All()
    {
        using var ctx = CreateContext("Dev_GetAll");
        ctx.Developers.AddRange(
            new Developer { Id = "d1", Name = "A", UserId = "u1", Slug = "a" },
            new Developer { Id = "d2", Name = "B", UserId = "u2", Slug = "b" }
        );
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new DeveloperService(uow);

        var devs = await service.GetAllAsync();

        devs.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetGamesAsync_Returns_Developer_Games()
    {
        using var ctx = CreateContext("Dev_Games");
        ctx.Games.AddRange(
            new Game { Id = "g1", Title = "Game A", DeveloperId = "d1", Price = 10m, ReleaseDate = DateTime.UtcNow },
            new Game { Id = "g2", Title = "Game B", DeveloperId = "d1", Price = 20m, ReleaseDate = DateTime.UtcNow }
        );
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new DeveloperService(uow);

        var games = await service.GetGamesAsync("d1");

        games.Should().HaveCount(2);
    }

    [Fact]
    public async Task IsDeveloperUserAsync_Returns_True_If_Developer()
    {
        using var ctx = CreateContext("Dev_IsDev");
        ctx.Developers.Add(new Developer { Id = "d1", Name = "Studio", UserId = "u1", Slug = "s" });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new DeveloperService(uow);

        var result = await service.IsDeveloperUserAsync("u1");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_Deletes_Developer()
    {
        using var ctx = CreateContext("Dev_Delete");
        ctx.Developers.Add(new Developer { Id = "d1", Name = "Studio", UserId = "u1", Slug = "s" });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new DeveloperService(uow);

        var (success, error) = await service.DeleteAsync("d1");

        success.Should().BeTrue();
        ctx.Developers.Should().BeEmpty();
    }

    [Fact]
    public async Task DemoteAsync_Demotes_Developer_To_Customer()
    {
        using var ctx = CreateContext("Dev_Demote");
        ctx.Users.Add(new User { Id = "u1", Username = "Alice", Email = "a@t.com", PasswordHash = "h", Role = Role.DEVELOPER });
        ctx.Developers.Add(new Developer { Id = "d1", Name = "Studio", UserId = "u1", Slug = "s", IsActive = true });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new DeveloperService(uow);

        var (success, error) = await service.DemoteAsync("d1");

        success.Should().BeTrue();
        ctx.Developers.Single().IsActive.Should().BeFalse();
        ctx.Users.Find("u1")!.Role.Should().Be(Role.CUSTOMER);
    }

    [Fact]
    public async Task DemoteAsync_Fails_If_Self_Demote()
    {
        using var ctx = CreateContext("Dev_DemoteSelf");
        ctx.Users.Add(new User { Id = "u1", Username = "Alice", Email = "a@t.com", PasswordHash = "h" });
        ctx.Developers.Add(new Developer { Id = "d1", Name = "Studio", UserId = "u1", Slug = "s" });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new DeveloperService(uow);

        var (success, error) = await service.DemoteAsync("d1", "u1");

        success.Should().BeFalse();
        error.Should().Be("Cannot demote yourself.");
    }

    [Fact]
    public async Task GetDashboardStatsAsync_Returns_Stats()
    {
        using var ctx = CreateContext("Dev_Dashboard");
        ctx.Games.AddRange(
            new Game { Id = "g1", Title = "A", DeveloperId = "d1", Price = 10m, ReleaseDate = DateTime.UtcNow },
            new Game { Id = "g2", Title = "B", DeveloperId = "d1", Price = 20m, ReleaseDate = DateTime.UtcNow }
        );
        ctx.Reviews.Add(new Review { Id = "r1", UserId = "u1", GameId = "g1", Rating = 4 });
        ctx.OrderItems.Add(new OrderItem { Id = "oi1", OrderId = "o1", GameId = "g1", PriceAtPurchase = 10m });
        ctx.LibraryGames.Add(new LibraryGame { LibraryId = "lib1", GameId = "g1" });
        ctx.Users.Add(new User { Id = "u1", Username = "A", Email = "a@t.com", PasswordHash = "h" });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new DeveloperService(uow);

        var stats = await service.GetDashboardStatsAsync("d1");

        stats.GameCount.Should().Be(2);
        stats.TotalRevenue.Should().Be(10);
        stats.NetRevenue.Should().Be(8); // 10 * 85% = 8.5 → (int)8
    }

    [Fact]
    public async Task GetGameStatsAsync_Returns_Game_Stats()
    {
        using var ctx = CreateContext("Dev_GameStats");
        ctx.Games.Add(new Game { Id = "g1", Title = "A", DeveloperId = "d1", Price = 10m, ReleaseDate = DateTime.UtcNow });
        ctx.Reviews.AddRange(
            new Review { Id = "r1", UserId = "u1", GameId = "g1", Rating = 4 },
            new Review { Id = "r2", UserId = "u2", GameId = "g1", Rating = 5 }
        );
        ctx.LibraryGames.Add(new LibraryGame { LibraryId = "lib1", GameId = "g1" });
        ctx.Users.Add(new User { Id = "u1", Username = "A", Email = "a@t.com", PasswordHash = "h" });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new DeveloperService(uow);

        var stats = await service.GetGameStatsAsync("d1");

        stats.Should().HaveCount(1);
        stats[0].Downloads.Should().Be(1);
        stats[0].AvgRating.Should().Be(4.5);
        stats[0].TotalRevenue.Should().Be(0);
    }

    [Fact]
    public async Task ReactivateAsync_Reactivates_Developer()
    {
        using var ctx = CreateContext("Dev_Reactivate");
        ctx.Users.Add(new User { Id = "u1", Username = "Alice", Email = "a@t.com", PasswordHash = "h", Role = Role.CUSTOMER });
        ctx.Developers.Add(new Developer { Id = "d1", Name = "Studio", UserId = "u1", Slug = "s", IsActive = false });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new DeveloperService(uow);

        var (success, error) = await service.ReactivateAsync("d1");

        success.Should().BeTrue();
        ctx.Developers.Single().IsActive.Should().BeTrue();
        ctx.Users.Find("u1")!.Role.Should().Be(Role.DEVELOPER);
    }
}
