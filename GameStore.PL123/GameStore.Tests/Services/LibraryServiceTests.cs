using FluentAssertions;

namespace GameStore.Tests.Services;

public class LibraryServiceTests
{
    private static GameStoreDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<GameStoreDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new GameStoreDbContext(options);
    }

    [Fact]
    public async Task GetLibraryGamesAsync_Returns_Library_Games()
    {
        using var ctx = CreateContext("Lib_Get");
        ctx.Users.Add(new User { Id = "u1", Username = "Alice", Email = "a@t.com", PasswordHash = "h" });
        ctx.Games.Add(new Game { Id = "g1", Title = "Game One", Price = 10m, ReleaseDate = DateTime.UtcNow });
        ctx.Libraries.Add(new Library { Id = "lib1", UserId = "u1" });
        ctx.LibraryGames.Add(new LibraryGame { LibraryId = "lib1", GameId = "g1" });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new LibraryService(uow);

        var games = await service.GetLibraryGamesAsync("u1");

        games.Should().HaveCount(1);
        games[0].GameId.Should().Be("g1");
    }

    [Fact]
    public async Task GetLibraryGamesAsync_Returns_Empty_If_No_Library()
    {
        using var ctx = CreateContext("Lib_GetEmpty");
        var uow = new UnitOfWork(ctx);
        var service = new LibraryService(uow);

        var games = await service.GetLibraryGamesAsync("nonexistent");

        games.Should().BeEmpty();
    }

    [Fact]
    public async Task HasGame_Returns_True_If_User_Has_Game()
    {
        using var ctx = CreateContext("Lib_HasGame");
        ctx.Users.Add(new User { Id = "u1", Username = "Alice", Email = "a@t.com", PasswordHash = "h" });
        ctx.Games.Add(new Game { Id = "g1", Title = "Game", Price = 10m, ReleaseDate = DateTime.UtcNow });
        ctx.Libraries.Add(new Library { Id = "lib1", UserId = "u1" });
        ctx.LibraryGames.Add(new LibraryGame { LibraryId = "lib1", GameId = "g1" });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new LibraryService(uow);

        var result = await service.HasGame("u1", "g1");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task AddGameToLibraryAsync_Adds_Game_To_Library()
    {
        using var ctx = CreateContext("Lib_Add");
        ctx.Users.Add(new User { Id = "u1", Username = "Alice", Email = "a@t.com", PasswordHash = "h" });
        ctx.Games.Add(new Game { Id = "g1", Title = "Game", Price = 10m, ReleaseDate = DateTime.UtcNow });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new LibraryService(uow);

        await service.AddGameToLibraryAsync("u1", "g1");

        ctx.Libraries.Should().HaveCount(1);
        ctx.LibraryGames.Should().HaveCount(1);
    }

    [Fact]
    public async Task AddGameToLibraryAsync_Creates_Library_If_Not_Exists()
    {
        using var ctx = CreateContext("Lib_AddNoLib");
        ctx.Users.Add(new User { Id = "u1", Username = "Alice", Email = "a@t.com", PasswordHash = "h" });
        ctx.Games.Add(new Game { Id = "g1", Title = "Game", Price = 10m, ReleaseDate = DateTime.UtcNow });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new LibraryService(uow);

        await service.AddGameToLibraryAsync("u1", "g1");

        ctx.Libraries.Should().HaveCount(1);
        ctx.LibraryGames.Should().HaveCount(1);
    }

    [Fact]
    public async Task AddGameToLibraryAsync_Does_Not_Duplicate()
    {
        using var ctx = CreateContext("Lib_AddDup");
        ctx.Users.Add(new User { Id = "u1", Username = "Alice", Email = "a@t.com", PasswordHash = "h" });
        ctx.Games.Add(new Game { Id = "g1", Title = "Game", Price = 10m, ReleaseDate = DateTime.UtcNow });
        ctx.Libraries.Add(new Library { Id = "lib1", UserId = "u1" });
        ctx.LibraryGames.Add(new LibraryGame { LibraryId = "lib1", GameId = "g1" });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new LibraryService(uow);

        await service.AddGameToLibraryAsync("u1", "g1");

        ctx.LibraryGames.Should().HaveCount(1);
    }

    [Fact]
    public async Task HasGame_Returns_False_If_Not_Owned()
    {
        using var ctx = CreateContext("Lib_NotOwned");
        ctx.Users.Add(new User { Id = "u1", Username = "Alice", Email = "a@t.com", PasswordHash = "h" });
        ctx.Games.Add(new Game { Id = "g1", Title = "Game", Price = 10m, ReleaseDate = DateTime.UtcNow });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new LibraryService(uow);

        var result = await service.HasGame("u1", "g1");

        result.Should().BeFalse();
    }
}
