using FluentAssertions;

namespace GameStore.Tests.Services;

public class ReviewServiceTests
{
    private static GameStoreDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<GameStoreDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new GameStoreDbContext(options);
    }

    private static async Task SeedBasic(GameStoreDbContext ctx)
    {
        ctx.Users.Add(new User { Id = "u1", Username = "Alice", Email = "a@t.com", PasswordHash = "h" });
        ctx.Games.Add(new Game { Id = "g1", Title = "Game A", Price = 10m, ReleaseDate = DateTime.UtcNow });
        await ctx.SaveChangesAsync();
    }

    [Fact]
    public async Task CreateAsync_Creates_Review()
    {
        using var ctx = CreateContext("Rev_Create");
        await SeedBasic(ctx);
        var uow = new UnitOfWork(ctx);
        var service = new ReviewService(uow);

        var (success, error) = await service.CreateAsync("u1", "g1", 4, "Great game!");

        success.Should().BeTrue();
        ctx.Reviews.Should().HaveCount(1);
        ctx.Reviews.Single().Rating.Should().Be(4);
        ctx.Reviews.Single().Comment.Should().Be("Great game!");
    }

    [Fact]
    public async Task CreateAsync_Fails_If_Duplicate_Review()
    {
        using var ctx = CreateContext("Rev_CreateDup");
        await SeedBasic(ctx);
        ctx.Reviews.Add(new Review { Id = "r1", UserId = "u1", GameId = "g1", Rating = 5 });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new ReviewService(uow);

        var (success, error) = await service.CreateAsync("u1", "g1", 3, null);

        success.Should().BeFalse();
        error.Should().Be("You already reviewed this game.");
    }

    [Fact]
    public async Task CreateAsync_Fails_If_Rating_Out_Of_Range()
    {
        using var ctx = CreateContext("Rev_CreateBadRating");
        await SeedBasic(ctx);
        var uow = new UnitOfWork(ctx);
        var service = new ReviewService(uow);

        var (success, error) = await service.CreateAsync("u1", "g1", 0, null);

        success.Should().BeFalse();
        error.Should().Be("Rating must be between 1 and 5.");
    }

    [Fact]
    public async Task GetByGameAsync_Returns_Game_Reviews()
    {
        using var ctx = CreateContext("Rev_ByGame");
        await SeedBasic(ctx);
        ctx.Reviews.Add(new Review { Id = "r1", UserId = "u1", GameId = "g1", Rating = 4 });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new ReviewService(uow);

        var reviews = await service.GetByGameAsync("g1");

        reviews.Should().HaveCount(1);
    }

    [Fact]
    public async Task DeleteAsync_Deletes_Review()
    {
        using var ctx = CreateContext("Rev_Delete");
        await SeedBasic(ctx);
        ctx.Reviews.Add(new Review { Id = "r1", UserId = "u1", GameId = "g1", Rating = 4 });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new ReviewService(uow);

        var result = await service.DeleteAsync("r1");

        result.Should().BeTrue();
        ctx.Reviews.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteAsync_Returns_False_If_Not_Found()
    {
        using var ctx = CreateContext("Rev_DeleteNF");
        var uow = new UnitOfWork(ctx);
        var service = new ReviewService(uow);

        var result = await service.DeleteAsync("nonexistent");

        result.Should().BeFalse();
    }
}
