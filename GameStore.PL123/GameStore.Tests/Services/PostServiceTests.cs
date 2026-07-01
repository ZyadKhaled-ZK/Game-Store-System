using FluentAssertions;

namespace GameStore.Tests.Services;

public class PostServiceTests
{
    private static GameStoreDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<GameStoreDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new GameStoreDbContext(options);
    }

    [Fact]
    public async Task CreateAsync_Creates_Post()
    {
        using var ctx = CreateContext("Post_Create");
        ctx.Users.Add(new User { Id = "u1", Username = "Alice", Email = "a@t.com", PasswordHash = "h" });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new PostService(uow);

        var post = await service.CreateAsync("u1", "Hello world!");

        post.Id.Should().NotBeNull();
        post.Content.Should().Be("Hello world!");
        post.UserId.Should().Be("u1");
        ctx.Posts.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetUserPostsAsync_Returns_Paginated_Posts()
    {
        using var ctx = CreateContext("Post_GetUserPosts");
        ctx.Users.Add(new User { Id = "u1", Username = "A", Email = "a@t.com", PasswordHash = "h" });
        for (var i = 1; i <= 5; i++)
            ctx.Posts.Add(new Post { Id = $"p{i}", UserId = "u1", Content = $"Post {i}", CreatedAt = DateTime.UtcNow.AddMinutes(-i) });
        ctx.Posts.Add(new Post { Id = "p_other", UserId = "u2", Content = "Other" });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new PostService(uow);

        var posts = await service.GetUserPostsAsync("u1", 1, 3);

        posts.Should().HaveCount(3);
        posts.Should().BeInDescendingOrder(p => p.CreatedAt);
    }

    [Fact]
    public async Task DeleteAsync_Deletes_Own_Post()
    {
        using var ctx = CreateContext("Post_Delete");
        ctx.Posts.Add(new Post { Id = "p1", UserId = "u1", Content = "Post" });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new PostService(uow);

        var result = await service.DeleteAsync("p1", "u1");

        result.Should().BeTrue();
        ctx.Posts.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteAsync_Returns_False_If_Not_Own_Post()
    {
        using var ctx = CreateContext("Post_DeleteNotOwn");
        ctx.Posts.Add(new Post { Id = "p1", UserId = "u1", Content = "Post" });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new PostService(uow);

        var result = await service.DeleteAsync("p1", "u2");

        result.Should().BeFalse();
        ctx.Posts.Should().HaveCount(1);
    }

    [Fact]
    public async Task DeleteAsync_Returns_False_If_Not_Found()
    {
        using var ctx = CreateContext("Post_DeleteNF");
        var uow = new UnitOfWork(ctx);
        var service = new PostService(uow);

        var result = await service.DeleteAsync("nonexistent", "u1");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetUserPostCountAsync_Returns_Count()
    {
        using var ctx = CreateContext("Post_Count");
        ctx.Posts.AddRange(
            new Post { Id = "p1", UserId = "u1", Content = "A" },
            new Post { Id = "p2", UserId = "u1", Content = "B" },
            new Post { Id = "p3", UserId = "u2", Content = "C" }
        );
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new PostService(uow);

        var count = await service.GetUserPostCountAsync("u1");

        count.Should().Be(2);
    }

    [Fact]
    public async Task GetLastPostTimeAsync_Returns_Latest_Post_Time()
    {
        using var ctx = CreateContext("Post_LastTime");
        ctx.Posts.AddRange(
            new Post { Id = "p1", UserId = "u1", Content = "A", CreatedAt = DateTime.UtcNow.AddHours(-2) },
            new Post { Id = "p2", UserId = "u1", Content = "B", CreatedAt = DateTime.UtcNow.AddHours(-1) }
        );
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new PostService(uow);

        var lastTime = await service.GetLastPostTimeAsync("u1");

        lastTime.Should().NotBeNull();
        lastTime.Should().BeCloseTo(DateTime.UtcNow.AddHours(-1), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetLastPostTimeAsync_Returns_Null_If_No_Posts()
    {
        using var ctx = CreateContext("Post_LastTimeNull");
        var uow = new UnitOfWork(ctx);
        var service = new PostService(uow);

        var lastTime = await service.GetLastPostTimeAsync("u1");

        lastTime.Should().BeNull();
    }
}
