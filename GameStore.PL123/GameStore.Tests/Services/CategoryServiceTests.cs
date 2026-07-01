using FluentAssertions;

namespace GameStore.Tests.Services;

public class CategoryServiceTests
{
    private static GameStoreDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<GameStoreDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new GameStoreDbContext(options);
    }

    [Fact]
    public async Task GetAllAsync_Returns_All_Categories()
    {
        using var ctx = CreateContext("Cat_GetAll");
        ctx.Categories.AddRange(
            new Category { Id = "c1", Name = "Action" },
            new Category { Id = "c2", Name = "RPG" }
        );
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new CategoryService(uow);

        var cats = await service.GetAllAsync();

        cats.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllWithGameCountAsync_Includes_Game_Counts()
    {
        using var ctx = CreateContext("Cat_WithCount");
        ctx.Categories.AddRange(
            new Category { Id = "c1", Name = "Action" },
            new Category { Id = "c2", Name = "RPG" }
        );
        ctx.Games.Add(new Game { Id = "g1", Title = "Game", Price = 10m, ReleaseDate = DateTime.UtcNow });
        ctx.GameCategories.Add(new GameCategory { GameId = "g1", CategoryId = "c1" });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new CategoryService(uow);

        var cats = await service.GetAllWithGameCountAsync();

        cats.Should().HaveCount(2);
        cats.Single(c => c.Id == "c1").GameCategories.Should().HaveCount(1);
        cats.Single(c => c.Id == "c2").GameCategories.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateAsync_Creates_Category()
    {
        using var ctx = CreateContext("Cat_Create");
        var uow = new UnitOfWork(ctx);
        var service = new CategoryService(uow);

        var (success, error) = await service.CreateAsync("Action");

        success.Should().BeTrue();
        ctx.Categories.Should().ContainSingle(c => c.Name == "Action");
    }

    [Fact]
    public async Task CreateAsync_Fails_If_Duplicate_Name()
    {
        using var ctx = CreateContext("Cat_CreateDup");
        ctx.Categories.Add(new Category { Id = "c1", Name = "Action" });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new CategoryService(uow);

        var (success, error) = await service.CreateAsync("Action");

        success.Should().BeFalse();
        error.Should().Contain("already exists");
    }

    [Fact]
    public async Task UpdateAsync_Updates_Name()
    {
        using var ctx = CreateContext("Cat_Update");
        ctx.Categories.Add(new Category { Id = "c1", Name = "Old" });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new CategoryService(uow);

        var result = await service.UpdateAsync("c1", "New Name");

        result.Should().BeTrue();
        ctx.Categories.Find("c1")!.Name.Should().Be("New Name");
    }

    [Fact]
    public async Task UpdateAsync_Returns_False_If_Not_Found()
    {
        using var ctx = CreateContext("Cat_UpdateNF");
        var uow = new UnitOfWork(ctx);
        var service = new CategoryService(uow);

        var result = await service.UpdateAsync("nonexistent", "X");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_Deletes_Category_If_No_Games()
    {
        using var ctx = CreateContext("Cat_Delete");
        ctx.Categories.Add(new Category { Id = "c1", Name = "Action" });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new CategoryService(uow);

        var result = await service.DeleteAsync("c1");

        result.Should().BeTrue();
        ctx.Categories.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteAsync_Fails_If_Category_Has_Games()
    {
        using var ctx = CreateContext("Cat_DeleteHasGames");
        ctx.Categories.Add(new Category { Id = "c1", Name = "Action" });
        ctx.Games.Add(new Game { Id = "g1", Title = "Game", Price = 10m, ReleaseDate = DateTime.UtcNow });
        ctx.GameCategories.Add(new GameCategory { GameId = "g1", CategoryId = "c1" });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new CategoryService(uow);

        var result = await service.DeleteAsync("c1");

        result.Should().BeFalse();
    }
}
