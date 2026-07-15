using FluentAssertions;

namespace GameStore.Tests.Services;

public class FriendSuggestionServiceTests
{
    private static GameStoreDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<GameStoreDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new GameStoreDbContext(options);
    }

    [Fact]
    public async Task GetSuggestionsAsync_NoLibraryGames_ReturnsEmpty()
    {
        using var ctx = CreateContext("FriendSugg_NoLib");
        ctx.Users.Add(new User { Id = "u1", Username = "Alice", Email = "a@test.com", PasswordHash = "h", Role = Role.CUSTOMER });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new FriendSuggestionService(uow);

        var result = await service.GetSuggestionsAsync("u1");

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSuggestionsAsync_NoMatchingCandidates_ReturnsEmpty()
    {
        using var ctx = CreateContext("FriendSugg_NoMatch");
        var game = new Game { Id = "g1", Title = "Game1", Price = 10m, DeveloperId = "d1" };
        ctx.Games.Add(game);
        ctx.Users.Add(new User { Id = "u1", Username = "Alice", Email = "a@test.com", PasswordHash = "h", Role = Role.CUSTOMER });
        var lib = new Library { Id = "lib1", UserId = "u1" };
        ctx.Libraries.Add(lib);
        ctx.LibraryGames.Add(new LibraryGame { LibraryId = "lib1", GameId = "g1" });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new FriendSuggestionService(uow);

        var result = await service.GetSuggestionsAsync("u1");

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSuggestionsAsync_MutualGames_ReturnsSuggestions()
    {
        using var ctx = CreateContext("FriendSugg_Mutual");
        var game1 = new Game { Id = "g1", Title = "Game1", Price = 10m, DeveloperId = "d1" };
        var game2 = new Game { Id = "g2", Title = "Game2", Price = 20m, DeveloperId = "d2" };
        ctx.Games.AddRange(game1, game2);

        ctx.Users.AddRange(
            new User { Id = "u1", Username = "Alice", Email = "a@test.com", PasswordHash = "h", Role = Role.CUSTOMER },
            new User { Id = "u2", Username = "Bob", Email = "b@test.com", PasswordHash = "h", Role = Role.CUSTOMER }
        );

        ctx.Libraries.AddRange(
            new Library { Id = "lib1", UserId = "u1" },
            new Library { Id = "lib2", UserId = "u2" }
        );
        ctx.LibraryGames.AddRange(
            new LibraryGame { LibraryId = "lib1", GameId = "g1" },
            new LibraryGame { LibraryId = "lib2", GameId = "g1" },
            new LibraryGame { LibraryId = "lib2", GameId = "g2" }
        );
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new FriendSuggestionService(uow);

        var result = await service.GetSuggestionsAsync("u1");

        result.Should().NotBeEmpty();
        result[0].User.Id.Should().Be("u2");
        result[0].MutualGamesCount.Should().Be(1);
    }

    [Fact]
    public async Task GetSuggestionsAsync_ExcludesCurrentUser()
    {
        using var ctx = CreateContext("FriendSugg_ExcludeSelf");
        var game = new Game { Id = "g1", Title = "Game1", Price = 10m, DeveloperId = "d1" };
        ctx.Games.Add(game);
        ctx.Users.Add(new User { Id = "u1", Username = "Alice", Email = "a@test.com", PasswordHash = "h", Role = Role.CUSTOMER });
        ctx.Libraries.Add(new Library { Id = "lib1", UserId = "u1" });
        ctx.LibraryGames.Add(new LibraryGame { LibraryId = "lib1", GameId = "g1" });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new FriendSuggestionService(uow);

        var result = await service.GetSuggestionsAsync("u1");

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSuggestionsAsync_ExcludesExistingFriends()
    {
        using var ctx = CreateContext("FriendSugg_ExcludeFriends");
        var game = new Game { Id = "g1", Title = "Game1", Price = 10m, DeveloperId = "d1" };
        ctx.Games.Add(game);
        ctx.Users.AddRange(
            new User { Id = "u1", Username = "Alice", Email = "a@test.com", PasswordHash = "h", Role = Role.CUSTOMER },
            new User { Id = "u2", Username = "Bob", Email = "b@test.com", PasswordHash = "h", Role = Role.CUSTOMER }
        );
        ctx.Libraries.AddRange(
            new Library { Id = "lib1", UserId = "u1" },
            new Library { Id = "lib2", UserId = "u2" }
        );
        ctx.LibraryGames.AddRange(
            new LibraryGame { LibraryId = "lib1", GameId = "g1" },
            new LibraryGame { LibraryId = "lib2", GameId = "g1" }
        );
        ctx.Friendships.Add(new Friendship
        {
            RequesterId = "u1", ReceiverId = "u2", Status = FriendshipStatus.Accepted
        });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new FriendSuggestionService(uow);

        var result = await service.GetSuggestionsAsync("u1");

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSuggestionsAsync_ExcludesAdminUsers()
    {
        using var ctx = CreateContext("FriendSugg_ExcludeAdmins");
        var game = new Game { Id = "g1", Title = "Game1", Price = 10m, DeveloperId = "d1" };
        ctx.Games.Add(game);
        ctx.Users.AddRange(
            new User { Id = "u1", Username = "Alice", Email = "a@test.com", PasswordHash = "h", Role = Role.CUSTOMER },
            new User { Id = "admin1", Username = "Admin", Email = "adm@test.com", PasswordHash = "h", Role = Role.ADMIN }
        );
        ctx.Libraries.AddRange(
            new Library { Id = "lib1", UserId = "u1" },
            new Library { Id = "lib2", UserId = "admin1" }
        );
        ctx.LibraryGames.AddRange(
            new LibraryGame { LibraryId = "lib1", GameId = "g1" },
            new LibraryGame { LibraryId = "lib2", GameId = "g1" }
        );
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new FriendSuggestionService(uow);

        var result = await service.GetSuggestionsAsync("u1");

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSuggestionsAsync_ExcludesPendingFriendRequests()
    {
        using var ctx = CreateContext("FriendSugg_ExcludePending");
        var game = new Game { Id = "g1", Title = "Game1", Price = 10m, DeveloperId = "d1" };
        ctx.Games.Add(game);
        ctx.Users.AddRange(
            new User { Id = "u1", Username = "Alice", Email = "a@test.com", PasswordHash = "h", Role = Role.CUSTOMER },
            new User { Id = "u2", Username = "Bob", Email = "b@test.com", PasswordHash = "h", Role = Role.CUSTOMER }
        );
        ctx.Libraries.AddRange(
            new Library { Id = "lib1", UserId = "u1" },
            new Library { Id = "lib2", UserId = "u2" }
        );
        ctx.LibraryGames.AddRange(
            new LibraryGame { LibraryId = "lib1", GameId = "g1" },
            new LibraryGame { LibraryId = "lib2", GameId = "g1" }
        );
        ctx.Friendships.Add(new Friendship
        {
            RequesterId = "u1", ReceiverId = "u2", Status = FriendshipStatus.Pending
        });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new FriendSuggestionService(uow);

        var result = await service.GetSuggestionsAsync("u1");

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSuggestionsAsync_RespectCountLimit()
    {
        using var ctx = CreateContext("FriendSugg_CountLimit");
        var game = new Game { Id = "g1", Title = "Game1", Price = 10m, DeveloperId = "d1" };
        ctx.Games.Add(game);
        ctx.Users.Add(new User { Id = "u1", Username = "Alice", Email = "a@test.com", PasswordHash = "h", Role = Role.CUSTOMER });
        for (int i = 2; i <= 10; i++)
        {
            ctx.Users.Add(new User { Id = $"u{i}", Username = $"User{i}", Email = $"u{i}@test.com", PasswordHash = "h", Role = Role.CUSTOMER });
            ctx.Libraries.Add(new Library { Id = $"lib{i}", UserId = $"u{i}" });
            ctx.LibraryGames.Add(new LibraryGame { LibraryId = $"lib{i}", GameId = "g1" });
        }
        ctx.Libraries.Add(new Library { Id = "lib1", UserId = "u1" });
        ctx.LibraryGames.Add(new LibraryGame { LibraryId = "lib1", GameId = "g1" });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new FriendSuggestionService(uow);

        var result = await service.GetSuggestionsAsync("u1", count: 3);

        result.Count.Should().BeLessOrEqualTo(3);
    }
}
