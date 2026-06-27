using System.ComponentModel.DataAnnotations;

namespace GameStore.PL.Models.Auth;

public class LoginViewModel
{
    [Required, EmailAddress, StringLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}
