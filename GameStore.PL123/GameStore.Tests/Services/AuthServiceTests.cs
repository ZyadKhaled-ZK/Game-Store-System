using FluentAssertions;

namespace GameStore.Tests.Services;

public class AuthServiceTests
{
    private static GameStoreDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<GameStoreDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new GameStoreDbContext(options);
    }

    [Fact]
    public async Task RegisterAsync_Creates_User()
    {
        using var ctx = CreateContext("Auth_Register");
        var uow = new UnitOfWork(ctx);
        var service = new AuthService(uow);

        var (success, error) = await service.RegisterAsync("Alice", "alice@test.com", "password123");

        success.Should().BeTrue();
        error.Should().BeEmpty();
        var user = ctx.Users.Single();
        user.Username.Should().Be("Alice");
        user.Email.Should().Be("alice@test.com");
        user.Role.Should().Be(Role.CUSTOMER);
    }

    [Fact]
    public async Task RegisterAsync_Fails_If_Email_Exists()
    {
        using var ctx = CreateContext("Auth_RegisterDup");
        ctx.Users.Add(new User { Id = "u1", Username = "Alice", Email = "alice@test.com", PasswordHash = "hash", Role = Role.CUSTOMER });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new AuthService(uow);

        var (success, error) = await service.RegisterAsync("Bob", "alice@test.com", "password123");

        success.Should().BeFalse();
        error.Should().Be("Email already registered.");
    }

    [Fact]
    public async Task LoginAsync_Returns_User_With_Valid_Credentials()
    {
        using var ctx = CreateContext("Auth_LoginOk");
        var hash = BCrypt.Net.BCrypt.HashPassword("correctpw");
        ctx.Users.Add(new User { Id = "u1", Username = "Alice", Email = "alice@test.com", PasswordHash = hash, Role = Role.CUSTOMER });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new AuthService(uow);

        var user = await service.LoginAsync("alice@test.com", "correctpw");

        user.Should().NotBeNull();
        user!.Username.Should().Be("Alice");
    }

    [Fact]
    public async Task LoginAsync_Returns_Null_With_Wrong_Password()
    {
        using var ctx = CreateContext("Auth_LoginFail");
        var hash = BCrypt.Net.BCrypt.HashPassword("correctpw");
        ctx.Users.Add(new User { Id = "u1", Username = "Alice", Email = "alice@test.com", PasswordHash = hash, Role = Role.CUSTOMER });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new AuthService(uow);

        var user = await service.LoginAsync("alice@test.com", "wrongpw");

        user.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_Returns_Null_If_User_Not_Found()
    {
        using var ctx = CreateContext("Auth_LoginNoUser");
        var uow = new UnitOfWork(ctx);
        var service = new AuthService(uow);

        var user = await service.LoginAsync("noone@test.com", "pw");

        user.Should().BeNull();
    }

    [Fact]
    public async Task ChangePasswordAsync_Changes_Password_Successfully()
    {
        using var ctx = CreateContext("Auth_ChangePw");
        var hash = BCrypt.Net.BCrypt.HashPassword("oldpw");
        ctx.Users.Add(new User { Id = "u1", Username = "Alice", Email = "alice@test.com", PasswordHash = hash, Role = Role.CUSTOMER });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new AuthService(uow);

        var (success, error) = await service.ChangePasswordAsync("u1", "oldpw", "newpw");

        success.Should().BeTrue();
        var user = ctx.Users.Find("u1");
        BCrypt.Net.BCrypt.Verify("newpw", user!.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task ChangePasswordAsync_Fails_With_Wrong_Current_Password()
    {
        using var ctx = CreateContext("Auth_ChangePwFail");
        var hash = BCrypt.Net.BCrypt.HashPassword("oldpw");
        ctx.Users.Add(new User { Id = "u1", Username = "Alice", Email = "alice@test.com", PasswordHash = hash, Role = Role.CUSTOMER });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new AuthService(uow);

        var (success, error) = await service.ChangePasswordAsync("u1", "wrongpw", "newpw");

        success.Should().BeFalse();
        error.Should().Be("Current password is incorrect.");
    }

    [Fact]
    public async Task GenerateResetTokenAsync_Creates_Token_For_Existing_Email()
    {
        using var ctx = CreateContext("Auth_GenToken");
        ctx.Users.Add(new User { Id = "u1", Username = "Alice", Email = "alice@test.com", PasswordHash = "hash", Role = Role.CUSTOMER });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new AuthService(uow);

        var (success, error, token) = await service.GenerateResetTokenAsync("alice@test.com");

        success.Should().BeTrue();
        token.Should().NotBeNull();
        ctx.PasswordResetTokens.Single().Token.Should().Be(token);
    }

    [Fact]
    public async Task GenerateResetTokenAsync_Does_Not_Reveal_If_Email_Not_Exists()
    {
        using var ctx = CreateContext("Auth_GenTokenNoEmail");
        var uow = new UnitOfWork(ctx);
        var service = new AuthService(uow);

        var (success, error, token) = await service.GenerateResetTokenAsync("noone@test.com");

        success.Should().BeFalse();
        error.Should().Contain("If that email exists");
        token.Should().BeNull();
    }

    [Fact]
    public async Task ResetPasswordAsync_Resets_Password_With_Valid_Token()
    {
        using var ctx = CreateContext("Auth_ResetPw");
        ctx.Users.Add(new User { Id = "u1", Username = "Alice", Email = "alice@test.com", PasswordHash = "oldhash", Role = Role.CUSTOMER });
        ctx.PasswordResetTokens.Add(new PasswordResetToken
        {
            Id = Guid.NewGuid().ToString(),
            UserId = "u1",
            Token = "valid-token",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            IsUsed = false
        });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new AuthService(uow);

        var (success, error) = await service.ResetPasswordAsync("valid-token", "newpw");

        success.Should().BeTrue();
        var user = ctx.Users.Find("u1");
        BCrypt.Net.BCrypt.Verify("newpw", user!.PasswordHash).Should().BeTrue();
        ctx.PasswordResetTokens.Single().IsUsed.Should().BeTrue();
    }

    [Fact]
    public async Task ResetPasswordAsync_Fails_With_Expired_Token()
    {
        using var ctx = CreateContext("Auth_ResetPwExpired");
        ctx.Users.Add(new User { Id = "u1", Username = "Alice", Email = "alice@test.com", PasswordHash = "oldhash", Role = Role.CUSTOMER });
        ctx.PasswordResetTokens.Add(new PasswordResetToken
        {
            Id = Guid.NewGuid().ToString(),
            UserId = "u1",
            Token = "expired-token",
            ExpiresAt = DateTime.UtcNow.AddHours(-1),
            IsUsed = false
        });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new AuthService(uow);

        var (success, error) = await service.ResetPasswordAsync("expired-token", "newpw");

        success.Should().BeFalse();
        error.Should().Be("Invalid or expired reset token.");
    }

    [Fact]
    public async Task UpdateEmailAsync_Updates_Email()
    {
        using var ctx = CreateContext("Auth_UpdateEmail");
        ctx.Users.Add(new User { Id = "u1", Username = "Alice", Email = "old@test.com", PasswordHash = "hash", Role = Role.CUSTOMER });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new AuthService(uow);

        var (success, error) = await service.UpdateEmailAsync("u1", "new@test.com");

        success.Should().BeTrue();
        ctx.Users.Find("u1")!.Email.Should().Be("new@test.com");
    }

    [Fact]
    public async Task UpdateEmailAsync_Fails_If_Email_Already_Used()
    {
        using var ctx = CreateContext("Auth_UpdateEmailDup");
        ctx.Users.AddRange(
            new User { Id = "u1", Username = "Alice", Email = "alice@test.com", PasswordHash = "hash", Role = Role.CUSTOMER },
            new User { Id = "u2", Username = "Bob", Email = "bob@test.com", PasswordHash = "hash", Role = Role.CUSTOMER }
        );
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new AuthService(uow);

        var (success, error) = await service.UpdateEmailAsync("u1", "bob@test.com");

        success.Should().BeFalse();
        error.Should().Be("Email already in use.");
    }
}
