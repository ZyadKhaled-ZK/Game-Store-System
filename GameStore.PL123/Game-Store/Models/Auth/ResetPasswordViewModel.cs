using System.ComponentModel.DataAnnotations;

namespace GameStore.PL.Models.Auth;

public class ResetPasswordViewModel
{
    [Required]
    public string Token { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 6), DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    [Required, Compare("NewPassword", ErrorMessage = "Passwords do not match."), DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;
}
