using FluentAssertions;

namespace GameStore.Tests.Services;

public class UserServiceTests
{
    private static GameStoreDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<GameStoreDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new GameStoreDbContext(options);
    }

    [Fact]
    public async Task GetAllAsync_Returns_All_Users()
    {
        using var ctx = CreateContext("User_GetAll");
        ctx.Users.AddRange(
            new User { Id = "u1", Username = "Alice", Email = "a@t.com", PasswordHash = "h", Role = Role.CUSTOMER },
            new User { Id = "u2", Username = "Bob", Email = "b@t.com", PasswordHash = "h", Role = Role.ADMIN }
        );
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new UserService(uow);

        var users = await service.GetAllAsync();

        users.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByIdAsync_Returns_User()
    {
        using var ctx = CreateContext("User_GetById");
        ctx.Users.Add(new User { Id = "u1", Username = "Alice", Email = "a@t.com", PasswordHash = "h" });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new UserService(uow);

        var user = await service.GetByIdAsync("u1");

        user.Should().NotBeNull();
        user!.Username.Should().Be("Alice");
    }

    [Fact]
    public async Task ChangeRoleAsync_Changes_Role()
    {
        using var ctx = CreateContext("User_ChangeRole");
        ctx.Users.Add(new User { Id = "u1", Username = "Alice", Email = "a@t.com", PasswordHash = "h", Role = Role.CUSTOMER });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new UserService(uow);

        var (success, error) = await service.ChangeRoleAsync("u1", Role.ADMIN);

        success.Should().BeTrue();
        ctx.Users.Find("u1")!.Role.Should().Be(Role.ADMIN);
    }

    [Fact]
    public async Task ChangeRoleAsync_Fails_If_Self()
    {
        using var ctx = CreateContext("User_ChangeRoleSelf");
        ctx.Users.Add(new User { Id = "u1", Username = "Alice", Email = "a@t.com", PasswordHash = "h", Role = Role.CUSTOMER });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new UserService(uow);

        var (success, error) = await service.ChangeRoleAsync("u1", Role.ADMIN, "u1");

        success.Should().BeFalse();
        error.Should().Be("Cannot change your own role.");
    }

    [Fact]
    public async Task DeleteAsync_Deletes_User()
    {
        using var ctx = CreateContext("User_Delete");
        ctx.Users.Add(new User { Id = "u1", Username = "Alice", Email = "a@t.com", PasswordHash = "h" });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new UserService(uow);

        var (success, error) = await service.DeleteAsync("u1");

        success.Should().BeTrue();
        ctx.Users.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteAsync_Fails_If_Self()
    {
        using var ctx = CreateContext("User_DeleteSelf");
        ctx.Users.Add(new User { Id = "u1", Username = "Alice", Email = "a@t.com", PasswordHash = "h" });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new UserService(uow);

        var (success, error) = await service.DeleteAsync("u1", "u1");

        success.Should().BeFalse();
        error.Should().Be("Cannot delete your own account.");
    }

    [Fact]
    public async Task GetTotalUsersAsync_Returns_Count()
    {
        using var ctx = CreateContext("User_TotalCount");
        ctx.Users.AddRange(
            new User { Id = "u1", Username = "A", Email = "a@t.com", PasswordHash = "h" },
            new User { Id = "u2", Username = "B", Email = "b@t.com", PasswordHash = "h" }
        );
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new UserService(uow);

        var count = await service.GetTotalUsersAsync();

        count.Should().Be(2);
    }

    [Fact]
    public async Task GetUserByUsernameAsync_Returns_User()
    {
        using var ctx = CreateContext("User_ByUsername");
        ctx.Users.Add(new User { Id = "u1", Username = "Alice", Email = "a@t.com", PasswordHash = "h" });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new UserService(uow);

        var user = await service.GetUserByUsernameAsync("Alice");

        user.Should().NotBeNull();
        user!.Email.Should().Be("a@t.com");
    }

    [Fact]
    public async Task SearchUsersAsync_Returns_Matching_Users()
    {
        using var ctx = CreateContext("User_Search");
        ctx.Users.AddRange(
            new User { Id = "u1", Username = "Alice", Email = "a@t.com", PasswordHash = "h" },
            new User { Id = "u2", Username = "Bob", Email = "b@t.com", PasswordHash = "h" },
            new User { Id = "u3", Username = "Alex", Email = "c@t.com", PasswordHash = "h" }
        );
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new UserService(uow);

        var results = await service.SearchUsersAsync("Al");

        results.Should().HaveCount(2);
        results.Select(u => u.Username).Should().Contain(new[] { "Alice", "Alex" });
    }

    [Fact]
    public async Task SearchUsersAsync_Returns_Empty_For_Empty_Query()
    {
        using var ctx = CreateContext("User_SearchEmpty");
        var uow = new UnitOfWork(ctx);
        var service = new UserService(uow);

        var results = await service.SearchUsersAsync("");

        results.Should().BeEmpty();
    }
}
