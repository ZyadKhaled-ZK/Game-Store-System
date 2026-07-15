using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;

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

    private static IConfiguration CreateConfig()
    {
        var config = new Mock<IConfiguration>();
        config.Setup(x => x["PasswordSalt"]).Returns("test-salt");
        return config.Object;
    }

    [Fact]
    public async Task RegisterAsync_Creates_User()
    {
        using var ctx = CreateContext("Auth_Register");
        var uow = new UnitOfWork(ctx);
        var service = new AuthService(uow, CreateConfig());

        var (success, error) = await service.RegisterAsync("Alice", "alice@test.com", "Passw0rd!");

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
        var service = new AuthService(uow, CreateConfig());

        var (success, error) = await service.RegisterAsync("Bobby", "alice@test.com", "Passw0rd!");

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
        var service = new AuthService(uow, CreateConfig());

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
        var service = new AuthService(uow, CreateConfig());

        var user = await service.LoginAsync("alice@test.com", "wrongpw");

        user.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_Returns_Null_If_User_Not_Found()
    {
        using var ctx = CreateContext("Auth_LoginNoUser");
        var uow = new UnitOfWork(ctx);
        var service = new AuthService(uow, CreateConfig());

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
        var service = new AuthService(uow, CreateConfig());

        var (success, error) = await service.ChangePasswordAsync("u1", "oldpw", "NewP@ssw0rd");

        success.Should().BeTrue();
        var user = ctx.Users.Find("u1");
        BCrypt.Net.BCrypt.Verify("NewP@ssw0rd", user!.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task ChangePasswordAsync_Fails_With_Wrong_Current_Password()
    {
        using var ctx = CreateContext("Auth_ChangePwFail");
        var hash = BCrypt.Net.BCrypt.HashPassword("oldpw");
        ctx.Users.Add(new User { Id = "u1", Username = "Alice", Email = "alice@test.com", PasswordHash = hash, Role = Role.CUSTOMER });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new AuthService(uow, CreateConfig());

        var (success, error) = await service.ChangePasswordAsync("u1", "wrongpw", "NewP@ssw0rd");

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
        var service = new AuthService(uow, CreateConfig());

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
        var service = new AuthService(uow, CreateConfig());

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
        var service = new AuthService(uow, CreateConfig());

        var (success, error) = await service.ResetPasswordAsync("valid-token", "NewP@ssw0rd");

        success.Should().BeTrue();
        var user = ctx.Users.Find("u1");
        BCrypt.Net.BCrypt.Verify("NewP@ssw0rd", user!.PasswordHash).Should().BeTrue();
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
        var service = new AuthService(uow, CreateConfig());

        var (success, error) = await service.ResetPasswordAsync("expired-token", "NewP@ssw0rd");

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
        var service = new AuthService(uow, CreateConfig());

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
        var service = new AuthService(uow, CreateConfig());

        var (success, error) = await service.UpdateEmailAsync("u1", "bob@test.com");

        success.Should().BeFalse();
        error.Should().Be("Email already in use.");
    }

    [Theory]
    [InlineData("Secure123!", "TC_REG_001 - Valid password with mixed chars")]
    [InlineData("P@ssw0rd", "TC_REG_002 - Exactly 8 chars boundary")]
    [InlineData("MyStr0ng!Pass123", "TC_REG_003 - Long valid password")]
    [InlineData("Pass@1234", "TC_REG_004 - 9 chars valid password")]
    [InlineData("Abc123!x", "TC_REG_005 - 8 chars mixed lowercase")]
    [InlineData("12345!@#", "TC_REG_014 - Only numbers and special chars")]
    [InlineData("Test123!", "TC_REG_015 - Standard valid password")]
    [InlineData("aaaaa1!b", "TC_REG_016 - Minimal requirements met")]
    [InlineData("P@ssw0rdP@ssw0rdP@ssw0rdP@ssw0rdP@ssw0rd", "TC_REG_020 - 40 char long password")]
    public async Task RegisterAsync_Accepts_Valid_Passwords(string password, string scenario)
    {
        using var ctx = CreateContext($"Auth_ValidPw_{scenario.GetHashCode()}");
        var uow = new UnitOfWork(ctx);
        var service = new AuthService(uow, CreateConfig());

        var (success, error) = await service.RegisterAsync("TestUser", $"{scenario}@test.com", password);

        success.Should().BeTrue(because: scenario);
        error.Should().BeEmpty();
    }

    [Fact]
    public async Task RegisterAsync_Rejects_Empty_Password_TC_REG_011()
    {
        using var ctx = CreateContext("Auth_EmptyPw");
        var uow = new UnitOfWork(ctx);
        var service = new AuthService(uow, CreateConfig());

        var (success, error) = await service.RegisterAsync("TestUser", "empty@test.com", "");

        success.Should().BeFalse();
        error.Should().Be("Password is required.");
    }

    [Fact]
    public async Task RegisterAsync_Rejects_Null_Password()
    {
        using var ctx = CreateContext("Auth_NullPw");
        var uow = new UnitOfWork(ctx);
        var service = new AuthService(uow, CreateConfig());

        var (success, error) = await service.RegisterAsync("TestUser", "null@test.com", null!);

        success.Should().BeFalse();
        error.Should().Be("Password is required.");
    }

    [Fact]
    public async Task RegisterAsync_Rejects_Whitespace_Only_Password_TC_REG_012()
    {
        using var ctx = CreateContext("Auth_SpacePw");
        var uow = new UnitOfWork(ctx);
        var service = new AuthService(uow, CreateConfig());

        var (success, error) = await service.RegisterAsync("TestUser", "space@test.com", "        ");

        success.Should().BeFalse();
        error.Should().Be("Password must contain at least one number.");
    }

    [Theory]
    [InlineData("Pass12!", "TC_REG_006 - 6 chars, too short")]
    [InlineData("Abcdef1", "TC_REG_007 - 7 chars, no special char")]
    [InlineData("T1!", "TC_REG_019 - 3 chars, way too short")]
    public async Task RegisterAsync_Rejects_Password_Too_Short(string password, string scenario)
    {
        using var ctx = CreateContext($"Auth_ShortPw_{scenario.GetHashCode()}");
        var uow = new UnitOfWork(ctx);
        var service = new AuthService(uow, CreateConfig());

        var (success, error) = await service.RegisterAsync("TestUser", $"{scenario}@test.com", password);

        success.Should().BeFalse(because: scenario);
        error.Should().Be("Password must be at least 8 characters long.");
    }

    [Theory]
    [InlineData("Password", "TC_REG_008 - No number, no special char")]
    [InlineData("abcdefgh", "TC_REG_013 - Lowercase only, no number/special")]
    public async Task RegisterAsync_Rejects_Password_Missing_Number_And_Special(string password, string scenario)
    {
        using var ctx = CreateContext($"Auth_NoNumSpec_{scenario.GetHashCode()}");
        var uow = new UnitOfWork(ctx);
        var service = new AuthService(uow, CreateConfig());

        var (success, error) = await service.RegisterAsync("TestUser", $"{scenario}@test.com", password);

        success.Should().BeFalse(because: scenario);
        error.Should().Be("Password must contain at least one number.");
    }

    [Theory]
    [InlineData("Test1234", "TC_REG_017 - Has number but no special char")]
    [InlineData("12345678", "TC_REG_009 - Only digits, no special char")]
    public async Task RegisterAsync_Rejects_Password_Missing_Special_Char(string password, string scenario)
    {
        using var ctx = CreateContext($"Auth_NoSpec_{scenario.GetHashCode()}");
        var uow = new UnitOfWork(ctx);
        var service = new AuthService(uow, CreateConfig());

        var (success, error) = await service.RegisterAsync("TestUser", $"{scenario}@test.com", password);

        success.Should().BeFalse(because: scenario);
        error.Should().Be("Password must contain at least one special character.");
    }

    [Theory]
    [InlineData("TestAbc!", "TC_REG_018 - Has special char but no number")]
    public async Task RegisterAsync_Rejects_Password_Missing_Number(string password, string scenario)
    {
        using var ctx = CreateContext($"Auth_NoNum_{scenario.GetHashCode()}");
        var uow = new UnitOfWork(ctx);
        var service = new AuthService(uow, CreateConfig());

        var (success, error) = await service.RegisterAsync("TestUser", $"{scenario}@test.com", password);

        success.Should().BeFalse(because: scenario);
        error.Should().Be("Password must contain at least one number.");
    }

    [Fact]
    public async Task RegisterAsync_Rejects_Special_Chars_Only_No_Number_TC_REG_010()
    {
        using var ctx = CreateContext("Auth_SpecialOnly");
        var uow = new UnitOfWork(ctx);
        var service = new AuthService(uow, CreateConfig());

        var (success, error) = await service.RegisterAsync("TestUser", "special@test.com", "@#$%^&*()");

        success.Should().BeFalse();
        error.Should().Be("Password must contain at least one number.");
    }

    [Fact]
    public async Task ChangePasswordAsync_Rejects_Weak_New_Password()
    {
        using var ctx = CreateContext("Auth_ChangePwWeak");
        var hash = BCrypt.Net.BCrypt.HashPassword("oldpw");
        ctx.Users.Add(new User { Id = "u1", Username = "Alice", Email = "alice@test.com", PasswordHash = hash, Role = Role.CUSTOMER });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new AuthService(uow, CreateConfig());

        var (success, error) = await service.ChangePasswordAsync("u1", "oldpw", "weak");

        success.Should().BeFalse();
        error.Should().Be("Password must be at least 8 characters long.");
    }

    [Fact]
    public async Task ResetPasswordAsync_Rejects_Weak_New_Password()
    {
        using var ctx = CreateContext("Auth_ResetPwWeak");
        ctx.Users.Add(new User { Id = "u1", Username = "Alice", Email = "alice@test.com", PasswordHash = "oldhash", Role = Role.CUSTOMER });
        ctx.PasswordResetTokens.Add(new PasswordResetToken
        {
            Id = Guid.NewGuid().ToString(),
            UserId = "u1",
            Token = "valid-token-weak",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            IsUsed = false
        });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new AuthService(uow, CreateConfig());

        var (success, error) = await service.ResetPasswordAsync("valid-token-weak", "nump@1");

        success.Should().BeFalse();
        error.Should().Be("Password must be at least 8 characters long.");
    }

    [Fact]
    public async Task RegisterAsync_Password_Without_Digit_Rejected()
    {
        using var ctx = CreateContext("Auth_NoDigit");
        var uow = new UnitOfWork(ctx);
        var service = new AuthService(uow, CreateConfig());

        var (success, error) = await service.RegisterAsync("TestUser", "nodigit@test.com", "Abcdefg!");

        success.Should().BeFalse();
        error.Should().Be("Password must contain at least one number.");
    }

    [Fact]
    public async Task RegisterAsync_Password_Without_Special_Rejected()
    {
        using var ctx = CreateContext("Auth_NoSpecial");
        var uow = new UnitOfWork(ctx);
        var service = new AuthService(uow, CreateConfig());

        var (success, error) = await service.RegisterAsync("TestUser", "nospecial@test.com", "Abcdefg1");

        success.Should().BeFalse();
        error.Should().Be("Password must contain at least one special character.");
    }

    [Fact]
    public async Task ValidatePassword_Static_Returns_Correct_Results()
    {
        var (valid1, err1) = AuthService.ValidatePassword("P@ssw0rd");
        valid1.Should().BeTrue();
        err1.Should().BeEmpty();

        var (valid2, err2) = AuthService.ValidatePassword("short");
        valid2.Should().BeFalse();
        err2.Should().Be("Password must be at least 8 characters long.");

        var (valid3, err3) = AuthService.ValidatePassword("NoSpecial1");
        valid3.Should().BeFalse();
        err3.Should().Be("Password must contain at least one special character.");

        var (valid4, err4) = AuthService.ValidatePassword("NoDigit@abc");
        valid4.Should().BeFalse();
        err4.Should().Be("Password must contain at least one number.");
    }

    [Theory]
    [InlineData("JohnDoe", "TC_USR_001 - Letters only")]
    [InlineData("User123", "TC_USR_002 - Letters and numbers")]
    [InlineData("ABCDE", "TC_USR_003 - 5 uppercase letters")]
    [InlineData("test99", "TC_USR_004 - Lowercase and numbers")]
    [InlineData("abcd", "TC_USR_005 - 4 lowercase letters")]
    [InlineData("12345", "TC_USR_006 - Numbers only")]
    [InlineData("aB3xY9z", "TC_USR_007 - Mixed case and numbers")]
    public void ValidateUsername_Accepts_Valid_Usernames(string username, string scenario)
    {
        var (valid, error) = AuthService.ValidateUsername(username);

        valid.Should().BeTrue(because: scenario);
        error.Should().BeEmpty();
    }

    [Theory]
    [InlineData("abc", "TC_USR_008 - Too short, 3 chars")]
    [InlineData("ab", "TC_USR_009 - Too short, 2 chars")]
    [InlineData("a", "TC_USR_010 - Too short, 1 char")]
    public void ValidateUsername_Rejects_Too_Short(string username, string scenario)
    {
        var (valid, error) = AuthService.ValidateUsername(username);

        valid.Should().BeFalse(because: scenario);
        error.Should().Be("Username must be at least 4 characters long.");
    }

    [Theory]
    [InlineData("user@name", "TC_USR_010 - Contains @")]
    [InlineData("user#123", "TC_USR_011 - Contains #")]
    [InlineData("user name", "TC_USR_012 - Contains space")]
    [InlineData("user-name", "TC_USR_013 - Contains hyphen")]
    [InlineData("user.name", "TC_USR_014 - Contains dot")]
    [InlineData("user!123", "TC_USR_015 - Contains !")]
    [InlineData("@#$%^&", "TC_USR_016 - Only special characters")]
    [InlineData("test用户", "TC_USR_017 - Contains unicode")]
    public void ValidateUsername_Rejects_Special_Characters(string username, string scenario)
    {
        var (valid, error) = AuthService.ValidateUsername(username);

        valid.Should().BeFalse(because: scenario);
        error.Should().Be("Username can only contain letters and numbers.");
    }

    [Theory]
    [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYZ", "TC_USR_018 - 26 uppercase letters")]
    [InlineData("123456789012345678901", "TC_USR_019 - 21 digits")]
    public void ValidateUsername_Rejects_Too_Long(string username, string scenario)
    {
        var (valid, error) = AuthService.ValidateUsername(username);

        valid.Should().BeFalse(because: scenario);
        error.Should().Be("Username must not exceed 20 characters.");
    }

    [Fact]
    public void ValidateUsername_Rejects_Empty_String()
    {
        var (valid, error) = AuthService.ValidateUsername("");

        valid.Should().BeFalse();
        error.Should().Be("Username is required.");
    }

    [Fact]
    public void ValidateUsername_Rejects_Whitespace_Only()
    {
        var (valid, error) = AuthService.ValidateUsername("   ");

        valid.Should().BeFalse();
        error.Should().Be("Username is required.");
    }

    [Fact]
    public void ValidateUsername_Rejects_Null()
    {
        var (valid, error) = AuthService.ValidateUsername(null!);

        valid.Should().BeFalse();
        error.Should().Be("Username is required.");
    }

    [Fact]
    public async Task RegisterAsync_Rejects_Username_With_Special_Chars()
    {
        using var ctx = CreateContext("Auth_BadUsername");
        var uow = new UnitOfWork(ctx);
        var service = new AuthService(uow, CreateConfig());

        var (success, error) = await service.RegisterAsync("user@name", "test@test.com", "Passw0rd!");

        success.Should().BeFalse();
        error.Should().Be("Username can only contain letters and numbers.");
    }

    [Fact]
    public async Task RegisterAsync_Rejects_Short_Username()
    {
        using var ctx = CreateContext("Auth_ShortUsername");
        var uow = new UnitOfWork(ctx);
        var service = new AuthService(uow, CreateConfig());

        var (success, error) = await service.RegisterAsync("abc", "test@test.com", "Passw0rd!");

        success.Should().BeFalse();
        error.Should().Be("Username must be at least 4 characters long.");
    }

    [Fact]
    public async Task RegisterAsync_Accepts_Alphanumeric_Username()
    {
        using var ctx = CreateContext("Auth_GoodUsername");
        var uow = new UnitOfWork(ctx);
        var service = new AuthService(uow, CreateConfig());

        var (success, error) = await service.RegisterAsync("Player123", "test@test.com", "Passw0rd!");

        success.Should().BeTrue();
        error.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateProfileAsync_Rejects_Username_With_Symbols()
    {
        using var ctx = CreateContext("Auth_UpdateBadUsername");
        ctx.Users.Add(new User { Id = "u1", Username = "Alice", Email = "a@test.com", PasswordHash = "hash", Role = Role.CUSTOMER });
        await ctx.SaveChangesAsync();
        var uow = new UnitOfWork(ctx);
        var service = new AuthService(uow, CreateConfig());

        var (success, error) = await service.UpdateProfileAsync("u1", "user#name", null);

        success.Should().BeFalse();
        error.Should().Be("Username can only contain letters and numbers.");
    }

    [Fact]
    public void ValidatePassword_Rejects_Password_Same_As_Email()
    {
        var (valid, error) = AuthService.ValidatePassword("user1@test.com", "user1@test.com");

        valid.Should().BeFalse();
        error.Should().Be("Password cannot be the same as your email.");
    }

    [Fact]
    public void ValidatePassword_Rejects_Password_Same_As_Email_CaseInsensitive()
    {
        var (valid, error) = AuthService.ValidatePassword("User1@Test.Com", "user1@test.com");

        valid.Should().BeFalse();
        error.Should().Be("Password cannot be the same as your email.");
    }

    [Fact]
    public void ValidatePassword_Accepts_Password_Different_From_Email()
    {
        var (valid, error) = AuthService.ValidatePassword("P@ssw0rd1", "user@test.com");

        valid.Should().BeTrue();
        error.Should().BeEmpty();
    }

    [Fact]
    public async Task RegisterAsync_Rejects_Password_Same_As_Email()
    {
        using var ctx = CreateContext("Auth_PwSameAsEmail");
        var uow = new UnitOfWork(ctx);
        var service = new AuthService(uow, CreateConfig());

        var (success, error) = await service.RegisterAsync("TestUser1", "test1@test.com", "test1@test.com");

        success.Should().BeFalse();
        error.Should().Be("Password cannot be the same as your email.");
    }

    [Fact]
    public async Task RegisterAsync_Rejects_Password_Same_As_Email_CaseInsensitive()
    {
        using var ctx = CreateContext("Auth_PwSameAsEmailCI");
        var uow = new UnitOfWork(ctx);
        var service = new AuthService(uow, CreateConfig());

        var (success, error) = await service.RegisterAsync("TestUser1", "user1@example.com", "USER1@EXAMPLE.COM");

        success.Should().BeFalse();
        error.Should().Be("Password cannot be the same as your email.");
    }

    [Fact]
    public void ValidatePassword_Rejects_Password_Same_As_Email_LocalPart()
    {
        var (valid, error) = AuthService.ValidatePassword("user1", "user1@test.com");

        valid.Should().BeFalse();
        error.Should().Be("Password cannot be the same as your email.");
    }

    [Fact]
    public void ValidatePassword_Rejects_Password_Same_As_Email_LocalPart_CaseInsensitive()
    {
        var (valid, error) = AuthService.ValidatePassword("USER1", "user1@test.com");

        valid.Should().BeFalse();
        error.Should().Be("Password cannot be the same as your email.");
    }

    [Fact]
    public async Task RegisterAsync_Rejects_Password_Same_As_Email_LocalPart()
    {
        using var ctx = CreateContext("Auth_PwSameAsLocal");
        var uow = new UnitOfWork(ctx);
        var service = new AuthService(uow, CreateConfig());

        var (success, error) = await service.RegisterAsync("TestUser1", "test1@example.com", "test1");

        success.Should().BeFalse();
        error.Should().Be("Password cannot be the same as your email.");
    }
}
