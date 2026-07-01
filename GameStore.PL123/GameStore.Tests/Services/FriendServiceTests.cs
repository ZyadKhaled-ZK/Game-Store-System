using FluentAssertions;

namespace GameStore.Tests.Services;

public class FriendServiceTests
{
    private static GameStoreDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<GameStoreDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new GameStoreDbContext(options);
    }

    private static async Task SeedUsersAsync(GameStoreDbContext context)
    {
        context.Users.AddRange(
            new User { Id = "u1", Username = "Alice", Email = "alice@test.com", PasswordHash = "hash" },
            new User { Id = "u2", Username = "Bob", Email = "bob@test.com", PasswordHash = "hash" },
            new User { Id = "u3", Username = "Charlie", Email = "charlie@test.com", PasswordHash = "hash" },
            new User { Id = "u4", Username = "Diana", Email = "diana@test.com", PasswordHash = "hash" }
        );
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task SendRequestAsync_Creates_Pending_Friendship()
    {
        using var ctx = CreateContext("Send_Pending");
        await SeedUsersAsync(ctx);
        var uow = new UnitOfWork(ctx);
        var service = new FriendService(uow);

        var (success, error) = await service.SendRequestAsync("u1", "Bob");

        success.Should().BeTrue();
        error.Should().Be("Friend request sent.");
        var friendship = ctx.Friendships.Single();
        friendship.RequesterId.Should().Be("u1");
        friendship.ReceiverId.Should().Be("u2");
        friendship.Status.Should().Be(FriendshipStatus.Pending);
    }

    [Fact]
    public async Task SendRequestAsync_Fails_If_User_Not_Found()
    {
        using var ctx = CreateContext("Send_NotFound");
        await SeedUsersAsync(ctx);
        var uow = new UnitOfWork(ctx);
        var service = new FriendService(uow);

        var (success, error) = await service.SendRequestAsync("u1", "NonExistent");

        success.Should().BeFalse();
        error.Should().Be("User not found.");
    }

    [Fact]
    public async Task SendRequestAsync_Fails_If_Self_Request()
    {
        using var ctx = CreateContext("Send_Self");
        await SeedUsersAsync(ctx);
        var uow = new UnitOfWork(ctx);
        var service = new FriendService(uow);

        var (success, error) = await service.SendRequestAsync("u1", "Alice");

        success.Should().BeFalse();
        error.Should().Be("You cannot add yourself as a friend.");
    }

    [Fact]
    public async Task SendRequestAsync_Fails_If_Already_Friends()
    {
        using var ctx = CreateContext("Send_AlreadyFriends");
        await SeedUsersAsync(ctx);
        ctx.Friendships.Add(new Friendship
        {
            Id = "f1",
            RequesterId = "u1",
            ReceiverId = "u2",
            Status = FriendshipStatus.Accepted
        });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new FriendService(uow);

        var (success, error) = await service.SendRequestAsync("u1", "Bob");

        success.Should().BeFalse();
        error.Should().Be("You are already friends with this user.");
    }

    [Fact]
    public async Task SendRequestAsync_Accepts_If_Pending_Exists_As_Receiver()
    {
        using var ctx = CreateContext("Send_PendingExistsReceiver");
        await SeedUsersAsync(ctx);
        ctx.Friendships.Add(new Friendship
        {
            Id = "f1",
            RequesterId = "u2",
            ReceiverId = "u1",
            Status = FriendshipStatus.Pending
        });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new FriendService(uow);

        var (success, error) = await service.SendRequestAsync("u1", "Bob");

        success.Should().BeTrue();
        error.Should().Be("Friend request accepted!");
        ctx.Friendships.Single().Status.Should().Be(FriendshipStatus.Accepted);
    }

    [Fact]
    public async Task SendRequestAsync_Resends_If_Previously_Rejected()
    {
        using var ctx = CreateContext("Send_Rejected");
        await SeedUsersAsync(ctx);
        ctx.Friendships.Add(new Friendship
        {
            Id = "f1",
            RequesterId = "u1",
            ReceiverId = "u2",
            Status = FriendshipStatus.Rejected
        });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new FriendService(uow);

        var (success, error) = await service.SendRequestAsync("u1", "Bob");

        success.Should().BeTrue();
        error.Should().Be("Friend request sent.");
        ctx.Friendships.Single().Status.Should().Be(FriendshipStatus.Pending);
    }

    [Fact]
    public async Task AcceptRequestAsync_Accepts_Pending_Request()
    {
        using var ctx = CreateContext("Accept_Pending");
        await SeedUsersAsync(ctx);
        ctx.Friendships.Add(new Friendship
        {
            Id = "f1",
            RequesterId = "u2",
            ReceiverId = "u1",
            Status = FriendshipStatus.Pending
        });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new FriendService(uow);

        var (success, error) = await service.AcceptRequestAsync("f1", "u1");

        success.Should().BeTrue();
        error.Should().Be("Friend request accepted!");
        ctx.Friendships.Single().Status.Should().Be(FriendshipStatus.Accepted);
    }

    [Fact]
    public async Task AcceptRequestAsync_Fails_If_Unauthorized()
    {
        using var ctx = CreateContext("Accept_Unauthorized");
        await SeedUsersAsync(ctx);
        ctx.Friendships.Add(new Friendship
        {
            Id = "f1",
            RequesterId = "u2",
            ReceiverId = "u1",
            Status = FriendshipStatus.Pending
        });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new FriendService(uow);

        var (success, error) = await service.AcceptRequestAsync("f1", "u3");

        success.Should().BeFalse();
        error.Should().Be("Unauthorized.");
    }

    [Fact]
    public async Task AcceptRequestAsync_Fails_If_Not_Pending()
    {
        using var ctx = CreateContext("Accept_NotPending");
        await SeedUsersAsync(ctx);
        ctx.Friendships.Add(new Friendship
        {
            Id = "f1",
            RequesterId = "u2",
            ReceiverId = "u1",
            Status = FriendshipStatus.Accepted
        });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new FriendService(uow);

        var (success, error) = await service.AcceptRequestAsync("f1", "u1");

        success.Should().BeFalse();
        error.Should().Be("Request is no longer pending.");
    }

    [Fact]
    public async Task RejectRequestAsync_Rejects_Pending_Request()
    {
        using var ctx = CreateContext("Reject_Pending");
        await SeedUsersAsync(ctx);
        ctx.Friendships.Add(new Friendship
        {
            Id = "f1",
            RequesterId = "u2",
            ReceiverId = "u1",
            Status = FriendshipStatus.Pending
        });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new FriendService(uow);

        var (success, error) = await service.RejectRequestAsync("f1", "u1");

        success.Should().BeTrue();
        error.Should().Be("Friend request rejected.");
        ctx.Friendships.Single().Status.Should().Be(FriendshipStatus.Rejected);
    }

    [Fact]
    public async Task RejectRequestAsync_Fails_If_Unauthorized()
    {
        using var ctx = CreateContext("Reject_Unauthorized");
        await SeedUsersAsync(ctx);
        ctx.Friendships.Add(new Friendship
        {
            Id = "f1",
            RequesterId = "u2",
            ReceiverId = "u1",
            Status = FriendshipStatus.Pending
        });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new FriendService(uow);

        var (success, error) = await service.RejectRequestAsync("f1", "u3");

        success.Should().BeFalse();
        error.Should().Be("Unauthorized.");
    }

    [Fact]
    public async Task RemoveFriendAsync_Removes_Friendship()
    {
        using var ctx = CreateContext("Remove_Friend");
        await SeedUsersAsync(ctx);
        ctx.Friendships.Add(new Friendship
        {
            Id = "f1",
            RequesterId = "u1",
            ReceiverId = "u2",
            Status = FriendshipStatus.Accepted
        });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new FriendService(uow);

        var (success, error) = await service.RemoveFriendAsync("f1", "u1");

        success.Should().BeTrue();
        error.Should().Be("Friend removed.");
        ctx.Friendships.Should().BeEmpty();
    }

    [Fact]
    public async Task RemoveFriendAsync_Fails_If_Unauthorized()
    {
        using var ctx = CreateContext("Remove_Unauthorized");
        await SeedUsersAsync(ctx);
        ctx.Friendships.Add(new Friendship
        {
            Id = "f1",
            RequesterId = "u1",
            ReceiverId = "u2",
            Status = FriendshipStatus.Accepted
        });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new FriendService(uow);

        var (success, error) = await service.RemoveFriendAsync("f1", "u3");

        success.Should().BeFalse();
        error.Should().Be("Unauthorized.");
    }

    [Fact]
    public async Task GetFriendsAsync_Returns_Only_Accepted_Friendships()
    {
        using var ctx = CreateContext("GetFriends_Accepted");
        await SeedUsersAsync(ctx);
        ctx.Friendships.AddRange(
            new Friendship { Id = "f1", RequesterId = "u1", ReceiverId = "u2", Status = FriendshipStatus.Accepted },
            new Friendship { Id = "f2", RequesterId = "u1", ReceiverId = "u3", Status = FriendshipStatus.Pending },
            new Friendship { Id = "f3", RequesterId = "u4", ReceiverId = "u1", Status = FriendshipStatus.Accepted }
        );
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new FriendService(uow);

        var friends = await service.GetFriendsAsync("u1");

        friends.Should().HaveCount(2);
        friends.All(f => f.Status == FriendshipStatus.Accepted).Should().BeTrue();
    }

    [Fact]
    public async Task GetPendingRequestsAsync_Returns_Pending_For_Receiver()
    {
        using var ctx = CreateContext("GetPending");
        await SeedUsersAsync(ctx);
        ctx.Friendships.Add(
            new Friendship { Id = "f1", RequesterId = "u2", ReceiverId = "u1", Status = FriendshipStatus.Pending }
        );
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new FriendService(uow);

        var requests = await service.GetPendingRequestsAsync("u1");

        requests.Should().HaveCount(1);
        requests[0].RequesterId.Should().Be("u2");
    }

    [Fact]
    public async Task GetFriendIdsAsync_Returns_All_Friend_Ids()
    {
        using var ctx = CreateContext("GetFriendIds");
        await SeedUsersAsync(ctx);
        ctx.Friendships.AddRange(
            new Friendship { Id = "f1", RequesterId = "u1", ReceiverId = "u2", Status = FriendshipStatus.Accepted },
            new Friendship { Id = "f2", RequesterId = "u3", ReceiverId = "u1", Status = FriendshipStatus.Accepted }
        );
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new FriendService(uow);

        var ids = await service.GetFriendIdsAsync("u1");

        ids.Should().Contain(new[] { "u2", "u3" });
        ids.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetSuggestionsAsync_Excludes_Friends_And_Self()
    {
        using var ctx = CreateContext("Suggestions");
        await SeedUsersAsync(ctx);
        ctx.Friendships.Add(
            new Friendship { Id = "f1", RequesterId = "u1", ReceiverId = "u2", Status = FriendshipStatus.Accepted }
        );
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new FriendSuggestionService(uow);

        var suggestions = await service.GetSuggestionsAsync("u1", 10);

        suggestions.Should().NotContain(s => s.User.Id == "u1");
        suggestions.Should().NotContain(s => s.User.Id == "u2");
    }
}
