using System.ComponentModel.DataAnnotations;

namespace GameStore.PL.Models.Auth;

public class RegisterViewModel
{
    [Required, StringLength(20, MinimumLength = 4)]
    [RegularExpression(@"^[a-zA-Z0-9]+$", ErrorMessage = "Username can only contain letters and numbers.")]
    [Remote("CheckUsername", "Auth", HttpMethod = "GET", ErrorMessage = "Username already taken.")]
    public string Username { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 6), DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}
