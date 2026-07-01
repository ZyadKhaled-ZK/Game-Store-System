using System.ComponentModel.DataAnnotations;

namespace GameStore.PL.Models.Auth;

public class CompleteRegistrationViewModel
{
    [Required, StringLength(100, MinimumLength = 3)]
    [Remote("CheckUsername", "Auth", HttpMethod = "GET", ErrorMessage = "Username already taken.")]
    public string Username { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Bio { get; set; }
}
