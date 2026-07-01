using FluentAssertions;

namespace GameStore.Tests.Services;

public class GameServiceTests
{
    private static GameStoreDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<GameStoreDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new GameStoreDbContext(options);
    }

    [Fact]
    public async Task GetAllWithCategoriesAsync_Returns_All_Games()
    {
        using var ctx = CreateContext("Game_GetAll");
        ctx.Games.AddRange(
            new Game { Id = "g1", Title = "Game A", ReleaseDate = DateTime.UtcNow, Price = 10m },
            new Game { Id = "g2", Title = "Game B", ReleaseDate = DateTime.UtcNow, Price = 20m }
        );
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new GameService(uow);

        var games = await service.GetAllWithCategoriesAsync();

        games.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPagedAsync_Returns_Paginated_Games()
    {
        using var ctx = CreateContext("Game_Paged");
        for (var i = 1; i <= 10; i++)
            ctx.Games.Add(new Game { Id = $"g{i}", Title = $"Game {i}", ReleaseDate = DateTime.UtcNow, Price = i });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new GameService(uow);

        var result = await service.GetPagedAsync(1, 3);

        result.Items.Should().HaveCount(3);
        result.TotalCount.Should().Be(10);
        result.TotalPages.Should().Be(4);
    }

    [Fact]
    public async Task GetPagedAsync_Clamps_PageSize()
    {
        using var ctx = CreateContext("Game_PagedClamp");
        for (var i = 1; i <= 150; i++)
            ctx.Games.Add(new Game { Id = $"g{i}", Title = $"Game {i}", ReleaseDate = DateTime.UtcNow, Price = i });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new GameService(uow);

        var result = await service.GetPagedAsync(1, 200);

        result.PageSize.Should().Be(100);
    }

    [Fact]
    public async Task GetByIdAsync_Returns_Game()
    {
        using var ctx = CreateContext("Game_GetById");
        ctx.Games.Add(new Game { Id = "g1", Title = "Game One", Price = 10m, ReleaseDate = DateTime.UtcNow });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new GameService(uow);

        var game = await service.GetByIdAsync("g1");

        game.Should().NotBeNull();
        game!.Title.Should().Be("Game One");
    }

    [Fact]
    public async Task GetByIdAsync_Returns_Null_If_Not_Found()
    {
        using var ctx = CreateContext("Game_GetByIdNF");
        var uow = new UnitOfWork(ctx);
        var service = new GameService(uow);

        var game = await service.GetByIdAsync("nonexistent");

        game.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_Creates_Game_With_Categories()
    {
        using var ctx = CreateContext("Game_Create");
        ctx.Categories.AddRange(
            new Category { Id = "c1", Name = "Action" },
            new Category { Id = "c2", Name = "RPG" }
        );
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new GameService(uow);

        var game = new Game { Id = "g1", Title = "New Game", Price = 30m, ReleaseDate = DateTime.UtcNow };
        var created = await service.CreateAsync(game, new List<string> { "c1", "c2" });

        created.Should().NotBeNull();
        ctx.GameCategories.Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdateAsync_Updates_Game_Fields_And_Categories()
    {
        using var ctx = CreateContext("Game_Update");
        ctx.Categories.Add(new Category { Id = "c1", Name = "Action" });
        ctx.Games.Add(new Game { Id = "g1", Title = "Old Title", Price = 10m, ReleaseDate = DateTime.UtcNow });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new GameService(uow);

        var updated = await service.UpdateAsync("g1", new Game { Title = "New Title", Price = 20m, ReleaseDate = DateTime.UtcNow }, new List<string> { "c1" });

        updated.Should().NotBeNull();
        updated!.Title.Should().Be("New Title");
        updated.Price.Should().Be(20m);
    }

    [Fact]
    public async Task UpdateAsync_Returns_Null_If_Not_Found()
    {
        using var ctx = CreateContext("Game_UpdateNF");
        var uow = new UnitOfWork(ctx);
        var service = new GameService(uow);

        var result = await service.UpdateAsync("nonexistent", new Game { Title = "X", Price = 0, ReleaseDate = DateTime.UtcNow }, new List<string>());

        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_Deletes_Game()
    {
        using var ctx = CreateContext("Game_Delete");
        ctx.Games.Add(new Game { Id = "g1", Title = "Game", Price = 10m, ReleaseDate = DateTime.UtcNow });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new GameService(uow);

        var result = await service.DeleteAsync("g1");

        result.Should().BeTrue();
        ctx.Games.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteAsync_Returns_False_If_Not_Found()
    {
        using var ctx = CreateContext("Game_DeleteNF");
        var uow = new UnitOfWork(ctx);
        var service = new GameService(uow);

        var result = await service.DeleteAsync("nonexistent");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetTotalGamesAsync_Returns_Count()
    {
        using var ctx = CreateContext("Game_TotalCount");
        ctx.Games.AddRange(
            new Game { Id = "g1", Title = "A", Price = 0, ReleaseDate = DateTime.UtcNow },
            new Game { Id = "g2", Title = "B", Price = 10m, ReleaseDate = DateTime.UtcNow }
        );
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new GameService(uow);

        var count = await service.GetTotalGamesAsync();

        count.Should().Be(2);
    }

    [Fact]
    public async Task GetGamesByCategoryAsync_Returns_Grouped_Games()
    {
        using var ctx = CreateContext("Game_ByCat");
        ctx.Categories.Add(new Category { Id = "c1", Name = "Action" });
        ctx.Games.AddRange(
            new Game { Id = "g1", Title = "Game A", Price = 10m, ReleaseDate = DateTime.UtcNow },
            new Game { Id = "g2", Title = "Game B", Price = 20m, ReleaseDate = DateTime.UtcNow }
        );
        ctx.GameCategories.AddRange(
            new GameCategory { GameId = "g1", CategoryId = "c1" },
            new GameCategory { GameId = "g2", CategoryId = "c1" }
        );
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new GameService(uow);

        var result = await service.GetGamesByCategoryAsync();

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetFreeGamesCountAsync_Returns_Free_Games_Only()
    {
        using var ctx = CreateContext("Game_FreeCount");
        ctx.Games.AddRange(
            new Game { Id = "g1", Title = "Free", Price = 0, ReleaseDate = DateTime.UtcNow },
            new Game { Id = "g2", Title = "Paid", Price = 10m, ReleaseDate = DateTime.UtcNow }
        );
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new GameService(uow);

        var count = await service.GetFreeGamesCountAsync();

        count.Should().Be(1);
    }
}
