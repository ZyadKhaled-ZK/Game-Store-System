using FluentAssertions;

namespace GameStore.Tests.Security;

public class PasswordSecurityTests
{
    [Theory]
    [InlineData("password")]
    [InlineData("12345678")]
    [InlineData("abcdefgh")]
    public void WeakPasswords_FailValidation(string password)
    {
        var (valid, _) = AuthService.ValidatePassword(password);

        valid.Should().BeFalse($"'{password}' should not pass validation");
    }

    [Theory]
    [InlineData("P@ssw0rd")]
    [InlineData("MyStr0ng!Pass")]
    [InlineData("Secure123!")]
    [InlineData("Ab1!cdef")]
    public void StrongPasswords_PassValidation(string password)
    {
        var (valid, _) = AuthService.ValidatePassword(password);

        valid.Should().BeTrue($"'{password}' should pass validation");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void NullOrEmptyPassword_ReturnsRequiredError(string? password)
    {
        var (valid, error) = AuthService.ValidatePassword(password!);

        valid.Should().BeFalse();
        error.Should().Contain("required");
    }

    [Fact]
    public void PasswordExactly7Characters_TooShort()
    {
        var (valid, error) = AuthService.ValidatePassword("Abcde1!");

        valid.Should().BeFalse();
        error.Should().Contain("8 characters");
    }

    [Fact]
    public void PasswordExactly8Characters_AtMinimum()
    {
        var (valid, _) = AuthService.ValidatePassword("Abcdef1!");

        valid.Should().BeTrue();
    }

    [Fact]
    public void PasswordWithoutNumber_FailsNumberCheck()
    {
        var (valid, error) = AuthService.ValidatePassword("Abcdefg!");

        valid.Should().BeFalse();
        error.Should().Contain("number");
    }

    [Fact]
    public void PasswordWithoutSpecialChar_FailsSpecialCheck()
    {
        var (valid, error) = AuthService.ValidatePassword("Abcdefg1");

        valid.Should().BeFalse();
        error.Should().Contain("special character");
    }

    [Fact]
    public void VeryLongPassword_PassesValidation()
    {
        var longPw = new string('A', 1000) + "1!";
        var (valid, _) = AuthService.ValidatePassword(longPw);

        valid.Should().BeTrue();
    }

    [Fact]
    public void PasswordWithUnicodeSpecialChars_PassesValidation()
    {
        var (valid, _) = AuthService.ValidatePassword("Test123\u00A1");

        valid.Should().BeTrue();
    }

    [Fact]
    public void PasswordWithSpaces_StillNeedsSpecialChars()
    {
        var (valid, _) = AuthService.ValidatePassword("Test 123!");

        valid.Should().BeTrue();
    }

    [Fact]
    public void BCryptHash_ProducesDifferentHashesForSamePassword()
    {
        var hash1 = AuthService.HashPassword("Test123!");
        var hash2 = AuthService.HashPassword("Test123!");

        hash1.Should().NotBe(hash2, "BCrypt uses random salts");
        hash1.Should().StartWith("$2");
    }

    [Fact]
    public void BCryptHash_VerifiesCorrectPassword()
    {
        var password = "SecureP@ss1";
        var hash = AuthService.HashPassword(password);

        BCrypt.Net.BCrypt.Verify(password, hash).Should().BeTrue();
    }

    [Fact]
    public void BCryptHash_RejectsWrongPassword()
    {
        var hash = AuthService.HashPassword("SecureP@ss1");

        BCrypt.Net.BCrypt.Verify("WrongP@ss1", hash).Should().BeFalse();
    }

    [Fact]
    public void Registration_PasswordValidationPrecedesHashing()
    {
        AuthService.ValidatePassword("weak").IsValid.Should().BeFalse();
        AuthService.ValidatePassword("Strong1!").IsValid.Should().BeTrue();
    }
}
