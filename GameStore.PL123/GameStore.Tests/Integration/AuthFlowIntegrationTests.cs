using FluentAssertions;

namespace GameStore.Tests.Integration;

public class AuthFlowIntegrationTests
{
    private static GameStoreDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<GameStoreDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new GameStoreDbContext(options);
    }

    private static AuthService CreateService(GameStoreDbContext ctx)
    {
        var config = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
        config.Setup(c => c["PasswordSalt"]).Returns("test-salt");
        return new AuthService(new UnitOfWork(ctx), config.Object);
    }

    [Fact]
    public async Task RegisterThenLogin_FullFlow()
    {
        using var ctx = CreateContext("Integ_RegLogin");
        var service = CreateService(ctx);

        var (regSuccess, _) = await service.RegisterAsync("Alice", "alice@test.com", "P@ssw0rd1");
        regSuccess.Should().BeTrue();

        var user = await service.LoginAsync("alice@test.com", "P@ssw0rd1");
        user.Should().NotBeNull();
        user!.Username.Should().Be("Alice");
    }

    [Fact]
    public async Task RegisterThenLogin_WrongPassword_ReturnsNull()
    {
        using var ctx = CreateContext("Integ_RegWrongPw");
        var service = CreateService(ctx);

        await service.RegisterAsync("Alice", "alice@test.com", "P@ssw0rd1");

        var user = await service.LoginAsync("alice@test.com", "WrongP@ss1");
        user.Should().BeNull();
    }

    [Fact]
    public async Task RegisterThenResetPassword_FullFlow()
    {
        using var ctx = CreateContext("Integ_ResetPw");
        var service = CreateService(ctx);

        await service.RegisterAsync("Alice", "alice@test.com", "P@ssw0rd1");

        var (tokenSuccess, _, token) = await service.GenerateResetTokenAsync("alice@test.com");
        tokenSuccess.Should().BeTrue();
        token.Should().NotBeNull();

        var (resetSuccess, _) = await service.ResetPasswordAsync(token!, "N3wP@ss!x");
        resetSuccess.Should().BeTrue();

        var user = await service.LoginAsync("alice@test.com", "N3wP@ss!x");
        user.Should().NotBeNull();
    }

    [Fact]
    public async Task RegisterThenResetPassword_ExpiredToken_Fails()
    {
        using var ctx = CreateContext("Integ_ExpiredToken");
        var service = CreateService(ctx);

        await service.RegisterAsync("Alice", "alice@test.com", "P@ssw0rd1");
        var (_, _, token) = await service.GenerateResetTokenAsync("alice@test.com");

        var resetToken = ctx.PasswordResetTokens.Single(t => t.Token == token);
        resetToken.ExpiresAt = DateTime.UtcNow.AddHours(-1);
        await ctx.SaveChangesAsync();

        var (success, error) = await service.ResetPasswordAsync(token!, "N3wP@ss!x");
        success.Should().BeFalse();
        error.Should().Contain("expired");
    }

    [Fact]
    public async Task RegisterThenChangePassword_FullFlow()
    {
        using var ctx = CreateContext("Integ_ChangePw");
        var service = CreateService(ctx);

        var (regSuccess, _) = await service.RegisterAsync("Alice", "alice@test.com", "P@ssw0rd1");
        regSuccess.Should().BeTrue();
        var user = ctx.Users.Single();
        user.Id.Should().NotBeNullOrEmpty();

        var (changeSuccess, _) = await service.ChangePasswordAsync(user.Id, "P@ssw0rd1", "N3wP@ss!x");
        changeSuccess.Should().BeTrue();

        var loggedIn = await service.LoginAsync("alice@test.com", "N3wP@ss!x");
        loggedIn.Should().NotBeNull();

        var oldLogin = await service.LoginAsync("alice@test.com", "P@ssw0rd1");
        oldLogin.Should().BeNull();
    }

    [Fact]
    public async Task RegisterDuplicateEmail_Rejected()
    {
        using var ctx = CreateContext("Integ_DupEmail");
        var service = CreateService(ctx);

        var (s1, _) = await service.RegisterAsync("Alice", "alice@test.com", "P@ssw0rd1");
        var (s2, error) = await service.RegisterAsync("Bobby", "alice@test.com", "P@ssw0rd2");

        s1.Should().BeTrue();
        s2.Should().BeFalse();
        error.Should().Contain("already registered");
    }

    [Fact]
    public async Task EmailVerification_TokenFlow()
    {
        using var ctx = CreateContext("Integ_EmailVerify");
        var service = CreateService(ctx);

        var (regSuccess, _) = await service.RegisterAsync("Alice", "alice@test.com", "P@ssw0rd1");
        regSuccess.Should().BeTrue();
        var userId = ctx.Users.Single().Id;

        var token = await service.CreateVerificationTokenAsync(userId);
        token.Should().NotBeNullOrEmpty();

        var (verified, verifiedId) = await service.ConsumeVerificationTokenAsync(token);
        verified.Should().BeTrue();
        verifiedId.Should().Be(userId);

        var (verifiedAgain, _) = await service.ConsumeVerificationTokenAsync(token);
        verifiedAgain.Should().BeFalse("token should be single-use");
    }

    [Fact]
    public async Task ExternalLogin_CreatesUserIfNotExists()
    {
        using var ctx = CreateContext("Integ_ExtLogin");
        var service = CreateService(ctx);

        var (user, isNew) = await service.GetOrCreateExternalUserAsync("ext@test.com", "ExtUser", "avatar.png");

        user.Should().NotBeNull();
        isNew.Should().BeTrue();
        user!.IsExternalAccount.Should().BeTrue();
        user.AvatarUrl.Should().Be("avatar.png");
    }

    [Fact]
    public async Task ExternalLogin_ExistingUser_ReturnsExisting()
    {
        using var ctx = CreateContext("Integ_ExtLoginExisting");
        var service = CreateService(ctx);

        var (first, isNew1) = await service.GetOrCreateExternalUserAsync("ext@test.com", "ExtUser", null);
        var (second, isNew2) = await service.GetOrCreateExternalUserAsync("ext@test.com", "ExtUser2", "new.png");

        first.Id.Should().Be(second.Id);
        isNew1.Should().BeTrue();
        isNew2.Should().BeFalse();
    }
}
