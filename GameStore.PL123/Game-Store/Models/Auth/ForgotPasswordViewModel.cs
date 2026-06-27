using System.ComponentModel.DataAnnotations;

namespace GameStore.PL.Models.Auth;

public class ForgotPasswordViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
}
