using FluentAssertions;

namespace GameStore.Tests.Services;

public class DeveloperApplicationServiceTests
{
    private static GameStoreDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<GameStoreDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new GameStoreDbContext(options);
    }

    [Fact]
    public async Task SubmitAsync_Creates_Pending_Application()
    {
        using var ctx = CreateContext("DA_Submit");
        ctx.Users.Add(new User { Id = "u1", Username = "Alice", Email = "a@t.com", PasswordHash = "h" });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var devService = new DeveloperService(uow);
        var userService = new UserService(uow);
        var service = new DeveloperApplicationService(uow, devService, userService);

        var (success, error) = await service.SubmitAsync("u1", "My Studio", "desc", null, null);

        success.Should().BeTrue();
        ctx.DeveloperApplications.Should().HaveCount(1);
        ctx.DeveloperApplications.Single().Status.Should().Be(ApplicationStatus.Pending);
    }

    [Fact]
    public async Task SubmitAsync_Fails_If_Already_Developer()
    {
        using var ctx = CreateContext("DA_SubmitDev");
        ctx.Users.Add(new User { Id = "u1", Username = "Alice", Email = "a@t.com", PasswordHash = "h" });
        ctx.Developers.Add(new Developer { Id = "d1", Name = "Studio", UserId = "u1", Slug = "s" });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var devService = new DeveloperService(uow);
        var userService = new UserService(uow);
        var service = new DeveloperApplicationService(uow, devService, userService);

        var (success, error) = await service.SubmitAsync("u1", "Studio", null, null, null);

        success.Should().BeFalse();
        error.Should().Be("You are already a developer.");
    }

    [Fact]
    public async Task SubmitAsync_Fails_If_Duplicate_Pending()
    {
        using var ctx = CreateContext("DA_SubmitDup");
        ctx.Users.Add(new User { Id = "u1", Username = "Alice", Email = "a@t.com", PasswordHash = "h" });
        ctx.DeveloperApplications.Add(new DeveloperApplication
        {
            Id = "a1", UserId = "u1", Name = "Studio", Status = ApplicationStatus.Pending
        });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var devService = new DeveloperService(uow);
        var userService = new UserService(uow);
        var service = new DeveloperApplicationService(uow, devService, userService);

        var (success, error) = await service.SubmitAsync("u1", "Studio", null, null, null);

        success.Should().BeFalse();
        error.Should().Be("You already have a pending application.");
    }

    [Fact]
    public async Task SubmitAsync_Fails_If_Name_Empty()
    {
        using var ctx = CreateContext("DA_SubmitNoName");
        var uow = new UnitOfWork(ctx);
        var devService = new DeveloperService(uow);
        var userService = new UserService(uow);
        var service = new DeveloperApplicationService(uow, devService, userService);

        var (success, error) = await service.SubmitAsync("u1", "", null, null, null);

        success.Should().BeFalse();
        error.Should().Be("Studio name is required.");
    }

    [Fact]
    public async Task GetByIdAsync_Returns_Application()
    {
        using var ctx = CreateContext("DA_GetById");
        ctx.Users.Add(new User { Id = "u1", Username = "A", Email = "a@t.com", PasswordHash = "h" });
        ctx.DeveloperApplications.Add(new DeveloperApplication { Id = "a1", UserId = "u1", Name = "Studio" });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var devService = new DeveloperService(uow);
        var userService = new UserService(uow);
        var service = new DeveloperApplicationService(uow, devService, userService);

        var app = await service.GetByIdAsync("a1");

        app.Should().NotBeNull();
        app!.Name.Should().Be("Studio");
    }

    [Fact]
    public async Task GetPendingAsync_Returns_Only_Pending()
    {
        using var ctx = CreateContext("DA_Pending");
        ctx.Users.Add(new User { Id = "u1", Username = "A", Email = "a@t.com", PasswordHash = "h" });
        ctx.DeveloperApplications.AddRange(
            new DeveloperApplication { Id = "a1", UserId = "u1", Name = "A", Status = ApplicationStatus.Pending },
            new DeveloperApplication { Id = "a2", UserId = "u1", Name = "B", Status = ApplicationStatus.Approved }
        );
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var devService = new DeveloperService(uow);
        var userService = new UserService(uow);
        var service = new DeveloperApplicationService(uow, devService, userService);

        var pending = await service.GetPendingAsync();

        pending.Should().HaveCount(1);
        pending[0].Id.Should().Be("a1");
    }

    [Fact]
    public async Task ApproveAsync_Approves_And_Creates_Developer()
    {
        using var ctx = CreateContext("DA_Approve");
        ctx.Users.Add(new User { Id = "u1", Username = "Alice", Email = "a@t.com", PasswordHash = "h", Role = Role.CUSTOMER });
        ctx.DeveloperApplications.Add(new DeveloperApplication
        {
            Id = "a1", UserId = "u1", Name = "My Studio",
            Description = "desc", Status = ApplicationStatus.Pending
        });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var devService = new DeveloperService(uow);
        var userService = new UserService(uow);
        var service = new DeveloperApplicationService(uow, devService, userService);

        var (success, error) = await service.ApproveAsync("a1");

        success.Should().BeTrue();
        ctx.DeveloperApplications.Single().Status.Should().Be(ApplicationStatus.Approved);
        ctx.Developers.Should().HaveCount(1);
        ctx.Developers.Single().Name.Should().Be("My Studio");
        ctx.Users.Find("u1")!.Role.Should().Be(Role.DEVELOPER);
    }

    [Fact]
    public async Task ApproveAsync_Fails_If_Not_Pending()
    {
        using var ctx = CreateContext("DA_ApproveNotPending");
        ctx.Users.Add(new User { Id = "u1", Username = "A", Email = "a@t.com", PasswordHash = "h" });
        ctx.DeveloperApplications.Add(new DeveloperApplication
        {
            Id = "a1", UserId = "u1", Name = "S", Status = ApplicationStatus.Approved
        });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var devService = new DeveloperService(uow);
        var userService = new UserService(uow);
        var service = new DeveloperApplicationService(uow, devService, userService);

        var (success, error) = await service.ApproveAsync("a1");

        success.Should().BeFalse();
        error.Should().Be("Application is no longer pending.");
    }

    [Fact]
    public async Task RejectAsync_Deletes_Application()
    {
        using var ctx = CreateContext("DA_Reject");
        ctx.DeveloperApplications.Add(new DeveloperApplication
        {
            Id = "a1", UserId = "u1", Name = "Studio", Status = ApplicationStatus.Pending
        });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var devService = new DeveloperService(uow);
        var userService = new UserService(uow);
        var service = new DeveloperApplicationService(uow, devService, userService);

        var (success, error) = await service.RejectAsync("a1");

        success.Should().BeTrue();
        ctx.DeveloperApplications.Should().BeEmpty();
    }
}
