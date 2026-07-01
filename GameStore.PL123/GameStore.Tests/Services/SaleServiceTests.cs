using FluentAssertions;

namespace GameStore.Tests.Services;

public class SaleServiceTests
{
    private static GameStoreDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<GameStoreDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new GameStoreDbContext(options);
    }

    [Fact]
    public async Task GetByIdAsync_Returns_Sale()
    {
        using var ctx = CreateContext("Sale_GetById");
        ctx.Games.Add(new Game { Id = "g1", Title = "Game", DeveloperId = "d1", Price = 10m, ReleaseDate = DateTime.UtcNow });
        ctx.Developers.Add(new Developer { Id = "d1", Name = "Dev", UserId = "u1", Slug = "dev" });
        ctx.Users.Add(new User { Id = "u1", Username = "U", Email = "u@t.com", PasswordHash = "h" });
        ctx.Sales.Add(new Sale { Id = "s1", GameId = "g1", DeveloperId = "d1", NewPrice = 5m, Status = SaleStatus.Pending, StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(7) });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new SaleService(uow);

        var sale = await service.GetByIdAsync("s1");

        sale.Should().NotBeNull();
        sale!.NewPrice.Should().Be(5m);
    }

    [Fact]
    public async Task GetByIdAsync_Returns_Null_If_Not_Found()
    {
        using var ctx = CreateContext("Sale_GetByIdNF");
        var uow = new UnitOfWork(ctx);
        var service = new SaleService(uow);

        var sale = await service.GetByIdAsync("nonexistent");

        sale.Should().BeNull();
    }

    [Fact]
    public async Task GetPendingAsync_Returns_Only_Pending()
    {
        using var ctx = CreateContext("Sale_Pending");
        ctx.Games.AddRange(
            new Game { Id = "g1", Title = "A", DeveloperId = "d1", Price = 10m, ReleaseDate = DateTime.UtcNow },
            new Game { Id = "g2", Title = "B", DeveloperId = "d1", Price = 10m, ReleaseDate = DateTime.UtcNow }
        );
        ctx.Developers.Add(new Developer { Id = "d1", Name = "Dev", UserId = "u1", Slug = "dev" });
        ctx.Users.Add(new User { Id = "u1", Username = "U", Email = "u@t.com", PasswordHash = "h" });
        ctx.Sales.AddRange(
            new Sale { Id = "s1", GameId = "g1", DeveloperId = "d1", NewPrice = 5m, Status = SaleStatus.Pending, StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(7) },
            new Sale { Id = "s2", GameId = "g2", DeveloperId = "d1", NewPrice = 3m, Status = SaleStatus.Approved, StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(7) }
        );
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new SaleService(uow);

        var pending = await service.GetPendingAsync();

        pending.Should().HaveCount(1);
        pending[0].Id.Should().Be("s1");
    }

    [Fact]
    public async Task GetByDeveloperAsync_Returns_Developer_Sales()
    {
        using var ctx = CreateContext("Sale_ByDev");
        ctx.Games.AddRange(
            new Game { Id = "g1", Title = "A", DeveloperId = "d1", Price = 10m, ReleaseDate = DateTime.UtcNow },
            new Game { Id = "g2", Title = "B", DeveloperId = "d2", Price = 10m, ReleaseDate = DateTime.UtcNow }
        );
        ctx.Developers.AddRange(
            new Developer { Id = "d1", Name = "Dev1", UserId = "u1", Slug = "dev1" },
            new Developer { Id = "d2", Name = "Dev2", UserId = "u2", Slug = "dev2" }
        );
        ctx.Users.AddRange(
            new User { Id = "u1", Username = "U1", Email = "u1@t.com", PasswordHash = "h" },
            new User { Id = "u2", Username = "U2", Email = "u2@t.com", PasswordHash = "h" }
        );
        ctx.Sales.AddRange(
            new Sale { Id = "s1", GameId = "g1", DeveloperId = "d1", NewPrice = 5m, Status = SaleStatus.Pending, StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(7) },
            new Sale { Id = "s2", GameId = "g2", DeveloperId = "d2", NewPrice = 3m, Status = SaleStatus.Pending, StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(7) }
        );
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new SaleService(uow);

        var sales = await service.GetByDeveloperAsync("d1");

        sales.Should().HaveCount(1);
        sales[0].Id.Should().Be("s1");
    }

    [Fact]
    public async Task GetActiveSalesByGameIdsAsync_Returns_Active_Only()
    {
        using var ctx = CreateContext("Sale_Active");
        var now = DateTime.UtcNow;
        ctx.Sales.AddRange(
            new Sale { Id = "s1", GameId = "g1", DeveloperId = "d1", NewPrice = 5m, Status = SaleStatus.Approved, StartDate = now.AddDays(-1), EndDate = now.AddDays(7) },
            new Sale { Id = "s2", GameId = "g2", DeveloperId = "d1", NewPrice = 3m, Status = SaleStatus.Pending, StartDate = now.AddDays(-1), EndDate = now.AddDays(7) },
            new Sale { Id = "s3", GameId = "g3", DeveloperId = "d1", NewPrice = 2m, Status = SaleStatus.Approved, StartDate = now.AddDays(-10), EndDate = now.AddDays(-1) }
        );
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new SaleService(uow);

        var active = await service.GetActiveSalesByGameIdsAsync(new List<string> { "g1", "g2", "g3" });

        active.Should().HaveCount(1);
        active[0].Id.Should().Be("s1");
    }

    [Fact]
    public async Task GetActiveSalesByGameIdsAsync_Returns_Empty_If_Null_GameIds()
    {
        using var ctx = CreateContext("Sale_ActiveNull");
        var uow = new UnitOfWork(ctx);
        var service = new SaleService(uow);

        var active = await service.GetActiveSalesByGameIdsAsync(null!);

        active.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateAsync_Creates_Sale_And_Cancels_Existing_Pending()
    {
        using var ctx = CreateContext("Sale_Create");
        ctx.Games.Add(new Game { Id = "g1", Title = "Game", DeveloperId = "d1", Price = 10m, ReleaseDate = DateTime.UtcNow });
        ctx.Sales.Add(new Sale { Id = "s_old", GameId = "g1", DeveloperId = "d1", NewPrice = 7m, Status = SaleStatus.Pending, StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(7) });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new SaleService(uow);

        var (success, _) = await service.CreateAsync("d1", "g1", 5m, DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(14));

        success.Should().BeTrue();
        ctx.Sales.Should().HaveCount(2);
        ctx.Sales.Single(s => s.Id == "s_old").Status.Should().Be(SaleStatus.Cancelled);
    }

    [Fact]
    public async Task CreateAsync_Fails_If_Negative_Price()
    {
        using var ctx = CreateContext("Sale_CreateNeg");
        var uow = new UnitOfWork(ctx);
        var service = new SaleService(uow);

        var (success, error) = await service.CreateAsync("d1", "g1", -1m, DateTime.UtcNow, DateTime.UtcNow.AddDays(7));

        success.Should().BeFalse();
        error.Should().Be("Price cannot be negative.");
    }

    [Fact]
    public async Task CreateAsync_Fails_If_EndDate_Before_StartDate()
    {
        using var ctx = CreateContext("Sale_CreateDate");
        var uow = new UnitOfWork(ctx);
        var service = new SaleService(uow);

        var (success, error) = await service.CreateAsync("d1", "g1", 5m, DateTime.UtcNow.AddDays(7), DateTime.UtcNow);

        success.Should().BeFalse();
        error.Should().Be("End date must be after start date.");
    }

    [Fact]
    public async Task CreateAsync_Fails_If_Game_Not_Found()
    {
        using var ctx = CreateContext("Sale_CreateNoGame");
        var uow = new UnitOfWork(ctx);
        var service = new SaleService(uow);

        var (success, error) = await service.CreateAsync("d1", "nonexistent", 5m, DateTime.UtcNow, DateTime.UtcNow.AddDays(7));

        success.Should().BeFalse();
        error.Should().Be("Game not found.");
    }

    [Fact]
    public async Task CreateAsync_Fails_If_Not_Own_Game()
    {
        using var ctx = CreateContext("Sale_CreateNotOwn");
        ctx.Games.Add(new Game { Id = "g1", Title = "Game", DeveloperId = "other_dev", Price = 10m, ReleaseDate = DateTime.UtcNow });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new SaleService(uow);

        var (success, error) = await service.CreateAsync("d1", "g1", 5m, DateTime.UtcNow, DateTime.UtcNow.AddDays(7));

        success.Should().BeFalse();
        error.Should().Be("You can only create sales for your own games.");
    }

    [Fact]
    public async Task ApproveAsync_Approves_Pending_Sale()
    {
        using var ctx = CreateContext("Sale_Approve");
        ctx.Sales.Add(new Sale { Id = "s1", GameId = "g1", DeveloperId = "d1", NewPrice = 5m, Status = SaleStatus.Pending, StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(7) });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new SaleService(uow);

        var (success, _) = await service.ApproveAsync("s1");

        success.Should().BeTrue();
        ctx.Sales.Find("s1")!.Status.Should().Be(SaleStatus.Approved);
        ctx.Sales.Find("s1")!.ApprovedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ApproveAsync_Fails_If_Not_Pending()
    {
        using var ctx = CreateContext("Sale_ApproveNotPending");
        ctx.Sales.Add(new Sale { Id = "s1", GameId = "g1", DeveloperId = "d1", NewPrice = 5m, Status = SaleStatus.Approved, StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(7) });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new SaleService(uow);

        var (success, error) = await service.ApproveAsync("s1");

        success.Should().BeFalse();
        error.Should().Be("Only pending sales can be approved.");
    }

    [Fact]
    public async Task ApproveAsync_Fails_If_Not_Found()
    {
        using var ctx = CreateContext("Sale_ApproveNF");
        var uow = new UnitOfWork(ctx);
        var service = new SaleService(uow);

        var (success, error) = await service.ApproveAsync("nonexistent");

        success.Should().BeFalse();
        error.Should().Be("Sale not found.");
    }

    [Fact]
    public async Task RejectAsync_Rejects_Pending_Sale()
    {
        using var ctx = CreateContext("Sale_Reject");
        ctx.Sales.Add(new Sale { Id = "s1", GameId = "g1", DeveloperId = "d1", NewPrice = 5m, Status = SaleStatus.Pending, StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(7) });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new SaleService(uow);

        var (success, _) = await service.RejectAsync("s1", "Too cheap");

        success.Should().BeTrue();
        ctx.Sales.Find("s1")!.Status.Should().Be(SaleStatus.Rejected);
        ctx.Sales.Find("s1")!.RejectReason.Should().Be("Too cheap");
    }

    [Fact]
    public async Task RejectAsync_Fails_If_Not_Pending()
    {
        using var ctx = CreateContext("Sale_RejectNotPending");
        ctx.Sales.Add(new Sale { Id = "s1", GameId = "g1", DeveloperId = "d1", NewPrice = 5m, Status = SaleStatus.Rejected, StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(7) });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new SaleService(uow);

        var (success, error) = await service.RejectAsync("s1", "reason");

        success.Should().BeFalse();
        error.Should().Be("Only pending sales can be rejected.");
    }
}
