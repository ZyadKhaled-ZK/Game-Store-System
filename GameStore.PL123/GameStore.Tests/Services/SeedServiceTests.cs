using FluentAssertions;

namespace GameStore.Tests.Services;

public class SeedServiceTests
{
    private static GameStoreDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<GameStoreDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new GameStoreDbContext(options);
    }

    [Fact]
    public async Task SeedAsync_SeedsDefaultCategories_WhenEmpty()
    {
        using var ctx = CreateContext("Seed_Categories");
        var uow = new UnitOfWork(ctx);
        var service = new SeedService(uow);

        await service.SeedAsync();

        ctx.Categories.Should().HaveCount(12);
        var names = ctx.Categories.Select(c => c.Name).ToList();
        names.Should().Contain("Action");
        names.Should().Contain("RPG");
        names.Should().Contain("Shooter");
    }

    [Fact]
    public async Task SeedAsync_SeedsDeveloperUser_WhenNoneExists()
    {
        using var ctx = CreateContext("Seed_Developer");
        var uow = new UnitOfWork(ctx);
        var service = new SeedService(uow);

        await service.SeedAsync();

        var dev = ctx.Users.SingleOrDefault(u => u.Role == Role.DEVELOPER);
        dev.Should().NotBeNull();
        dev!.Username.Should().Be("Developer");
        dev.Email.Should().Be("dev@gamestore.com");
    }

    [Fact]
    public async Task SeedAsync_SeedsAdminUser_WhenNoneExists()
    {
        using var ctx = CreateContext("Seed_Admin");
        var uow = new UnitOfWork(ctx);
        var service = new SeedService(uow);

        await service.SeedAsync();

        var admin = ctx.Users.SingleOrDefault(u => u.Role == Role.ADMIN);
        admin.Should().NotBeNull();
        admin!.Username.Should().Be("Admin");
        admin.Email.Should().Be("admin@gamestore.com");
    }

    [Fact]
    public async Task SeedAsync_DoesNotDuplicate_WhenAlreadySeeded()
    {
        using var ctx = CreateContext("Seed_NoDuplicate");
        var uow = new UnitOfWork(ctx);
        var service = new SeedService(uow);

        await service.SeedAsync();
        await service.SeedAsync();

        ctx.Users.Count(u => u.Role == Role.DEVELOPER).Should().Be(1);
        ctx.Users.Count(u => u.Role == Role.ADMIN).Should().Be(1);
    }

    [Fact]
    public async Task SeedAsync_DoesNotOverwriteExistingDeveloper()
    {
        using var ctx = CreateContext("Seed_PreserveDev");
        ctx.Users.Add(new User
        {
            Id = "existing-dev",
            Username = "CustomDev",
            Email = "custom@dev.com",
            PasswordHash = "existing-hash",
            Role = Role.DEVELOPER
        });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new SeedService(uow);

        await service.SeedAsync();

        ctx.Users.Count(u => u.Role == Role.DEVELOPER).Should().Be(1);
        ctx.Users.Single(u => u.Role == Role.DEVELOPER).Username.Should().Be("CustomDev");
    }

    [Fact]
    public async Task SeedAsync_DoesNotOverwriteExistingAdmin()
    {
        using var ctx = CreateContext("Seed_PreserveAdmin");
        ctx.Users.Add(new User
        {
            Id = "existing-admin",
            Username = "SuperAdmin",
            Email = "super@admin.com",
            PasswordHash = "existing-hash",
            Role = Role.ADMIN
        });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new SeedService(uow);

        await service.SeedAsync();

        ctx.Users.Count(u => u.Role == Role.ADMIN).Should().Be(1);
        ctx.Users.Single(u => u.Role == Role.ADMIN).Username.Should().Be("SuperAdmin");
    }
}
