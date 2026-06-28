using FluentAssertions;

namespace GameStore.Tests.Services;

public class GameFileServiceTests
{
    private static GameStoreDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<GameStoreDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new GameStoreDbContext(options);
    }

    [Fact]
    public async Task UpdateGameFileAsync_Sets_File_Info()
    {
        using var ctx = CreateContext("GF_Update");
        ctx.Games.Add(new Game { Id = "g1", Title = "Game", Price = 10m, ReleaseDate = DateTime.UtcNow });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new GameFileService(uow);

        await service.UpdateGameFileAsync("g1", "/files/game.zip", "game.zip", 1024);

        var game = ctx.Games.Find("g1")!;
        game.GameFileUrl.Should().Be("/files/game.zip");
        game.GameFileName.Should().Be("game.zip");
        game.GameFileSizeBytes.Should().Be(1024);
    }

    [Fact]
    public async Task UpdateGameFileAsync_Does_Nothing_If_Game_Not_Found()
    {
        using var ctx = CreateContext("GF_UpdateNF");
        var uow = new UnitOfWork(ctx);
        var service = new GameFileService(uow);

        await service.UpdateGameFileAsync("nonexistent", "/f.zip", "f.zip", 100);
    }

    [Fact]
    public async Task ClearGameFileAsync_Clears_File_Info()
    {
        using var ctx = CreateContext("GF_Clear");
        ctx.Games.Add(new Game
        {
            Id = "g1", Title = "Game", Price = 10m, ReleaseDate = DateTime.UtcNow,
            GameFileUrl = "/f.zip", GameFileName = "f.zip", GameFileSizeBytes = 100
        });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new GameFileService(uow);

        await service.ClearGameFileAsync("g1");

        var game = ctx.Games.Find("g1")!;
        game.GameFileUrl.Should().BeNull();
        game.GameFileName.Should().BeNull();
        game.GameFileSizeBytes.Should().Be(0);
    }

    [Fact]
    public async Task AddScreenshotAsync_Adds_Url_If_New()
    {
        using var ctx = CreateContext("GF_AddScr");
        ctx.Games.Add(new Game { Id = "g1", Title = "Game", Price = 10m, ReleaseDate = DateTime.UtcNow });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new GameFileService(uow);

        await service.AddScreenshotAsync("g1", "https://example.com/shot1.png");

        ctx.Games.Find("g1")!.ScreenshotUrls.Should().Contain("https://example.com/shot1.png");
    }

    [Fact]
    public async Task AddScreenshotAsync_Does_Not_Duplicate()
    {
        using var ctx = CreateContext("GF_AddScrDup");
        ctx.Games.Add(new Game { Id = "g1", Title = "Game", Price = 10m, ReleaseDate = DateTime.UtcNow });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new GameFileService(uow);

        await service.AddScreenshotAsync("g1", "https://example.com/shot1.png");
        await service.AddScreenshotAsync("g1", "https://example.com/shot1.png");

        ctx.Games.Find("g1")!.ScreenshotUrls.Should().HaveCount(1);
    }

    [Fact]
    public async Task RemoveScreenshotAsync_Removes_Url()
    {
        using var ctx = CreateContext("GF_RemoveScr");
        ctx.Games.Add(new Game { Id = "g1", Title = "Game", Price = 10m, ReleaseDate = DateTime.UtcNow });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new GameFileService(uow);

        await service.AddScreenshotAsync("g1", "shot1.png");
        await service.RemoveScreenshotAsync("g1", "shot1.png");

        ctx.Games.Find("g1")!.ScreenshotUrls.Should().BeEmpty();
    }
}
