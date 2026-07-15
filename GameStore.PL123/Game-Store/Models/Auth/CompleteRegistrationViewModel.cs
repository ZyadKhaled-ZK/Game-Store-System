using System.ComponentModel.DataAnnotations;

namespace GameStore.PL.Models.Auth;

public class CompleteRegistrationViewModel
{
    [Required, StringLength(20, MinimumLength = 4)]
    [RegularExpression(@"^[a-zA-Z0-9]+$", ErrorMessage = "Username can only contain letters and numbers.")]
    [Remote("CheckUsername", "Auth", HttpMethod = "GET", ErrorMessage = "Username already taken.")]
    public string Username { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Bio { get; set; }
}
