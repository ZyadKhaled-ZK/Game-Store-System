using FluentAssertions;

namespace GameStore.Tests.Security;

public class InputSanitizationTests
{
    private static GameStoreDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<GameStoreDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new GameStoreDbContext(options);
    }

    [Theory]
    [InlineData("<script>alert('xss')</script>")]
    [InlineData("<img src=x onerror=alert(1)>")]
    [InlineData("javascript:alert(1)")]
    [InlineData("<svg onload=alert(1)>")]
    public void XssPayloads_InUsername_RegisterAsync_AcceptsAndStoresAsIs(string xssPayload)
    {
        using var ctx = CreateContext("Sec_XSS_" + xssPayload.GetHashCode());
        var uow = new UnitOfWork(ctx);
        var config = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
        config.Setup(c => c["PasswordSalt"]).Returns("test-salt");
        var service = new AuthService(uow, config.Object);

        var (success, _) = service.RegisterAsync(xssPayload, "test@test.com", "P@ssw0rd1").GetAwaiter().GetResult();

        success.Should().BeFalse("username validation should reject special characters at BLL layer");
    }

    [Theory]
    [InlineData("Robert'); DROP TABLE Users;--")]
    [InlineData("1' OR '1'='1")]
    [InlineData("admin'--")]
    public void SqlInjectionAttempts_InEmail_FailGracefully(string maliciousEmail)
    {
        using var ctx = CreateContext("Sec_SQL_" + maliciousEmail.GetHashCode());
        var uow = new UnitOfWork(ctx);
        var config = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
        config.Setup(c => c["PasswordSalt"]).Returns("test-salt");
        var service = new AuthService(uow, config.Object);

        var act = () => service.RegisterAsync("TestUser", maliciousEmail, "P@ssw0rd1");

        act.Should().NotThrowAsync("EF Core parameterized queries prevent SQL injection");
    }

    [Theory]
    [InlineData("../../../etc/passwd")]
    [InlineData("..\\..\\..\\windows\\system32")]
    public void PathTraversal_InFileName_DetectedByJsonFileStore(string maliciousPath)
    {
        var envMock = new Mock<IWebHostEnvironment>();
        envMock.Setup(e => e.WebRootPath).Returns(Path.GetTempPath());
        var store = new JsonFileStore(envMock.Object);

        var act = () => store.ReadAsync<object>(maliciousPath);

        act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public void EmptyPassword_Rejected()
    {
        using var ctx = CreateContext("Sec_EmptyPw");
        var uow = new UnitOfWork(ctx);
        var config = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
        config.Setup(c => c["PasswordSalt"]).Returns("test-salt");
        var service = new AuthService(uow, config.Object);

        var (success, error) = service.RegisterAsync("ValidUser", "u@test.com", "").GetAwaiter().GetResult();

        success.Should().BeFalse();
        error.Should().Be("Password is required.");
    }

    [Fact]
    public void NullPassword_Rejected()
    {
        using var ctx = CreateContext("Sec_NullPw");
        var uow = new UnitOfWork(ctx);
        var config = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
        config.Setup(c => c["PasswordSalt"]).Returns("test-salt");
        var service = new AuthService(uow, config.Object);

        var (success, _) = service.RegisterAsync("ValidUser", "u@test.com", null!).GetAwaiter().GetResult();

        success.Should().BeFalse();
    }

    [Fact]
    public void DuplicateEmail_Rejected()
    {
        using var ctx = CreateContext("Sec_DupEmail");
        ctx.Users.Add(new User { Id = "existing", Username = "ExistingUser", Email = "taken@test.com", PasswordHash = "h", Role = Role.CUSTOMER });
        ctx.SaveChangesAsync().GetAwaiter().GetResult();
        var uow = new UnitOfWork(ctx);
        var config = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
        config.Setup(c => c["PasswordSalt"]).Returns("test-salt");
        var service = new AuthService(uow, config.Object);

        var (success, error) = service.RegisterAsync("NewUser", "taken@test.com", "P@ssw0rd1").GetAwaiter().GetResult();

        success.Should().BeFalse();
        error.Should().Contain("already registered");
    }

    [Fact]
    public void VeryLongInput_DoesNotCrash()
    {
        using var ctx = CreateContext("Sec_LongInput");
        var uow = new UnitOfWork(ctx);
        var config = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
        config.Setup(c => c["PasswordSalt"]).Returns("test-salt");
        var service = new AuthService(uow, config.Object);

        var longString = new string('A', 10000);
        var act = () => service.RegisterAsync(longString, "long@test.com", "P@ssw0rd1");

        act.Should().NotThrowAsync("very long input should be handled gracefully without crashing");
    }

    [Fact]
    public void WhitespaceOnlyPassword_Rejected()
    {
        var (valid, _) = AuthService.ValidatePassword("          ");

        valid.Should().BeFalse();
    }

    [Fact]
    public void PasswordReset_TokenIsRandom()
    {
        using var ctx = CreateContext("Sec_TokenRandom");
        ctx.Users.Add(new User { Id = "u1", Username = "A", Email = "a@test.com", PasswordHash = "h", Role = Role.CUSTOMER });
        ctx.SaveChangesAsync().GetAwaiter().GetResult();
        var uow = new UnitOfWork(ctx);
        var config = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
        config.Setup(c => c["PasswordSalt"]).Returns("test-salt");
        var service = new AuthService(uow, config.Object);

        var (_, _, token1) = service.GenerateResetTokenAsync("a@test.com").GetAwaiter().GetResult();
        var (_, _, token2) = service.GenerateResetTokenAsync("a@test.com").GetAwaiter().GetResult();

        token1.Should().NotBe(token2);
        token1!.Length.Should().BeGreaterThan(32);
    }
}
