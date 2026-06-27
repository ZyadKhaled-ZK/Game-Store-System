using System.ComponentModel.DataAnnotations;

namespace GameStore.PL.Models.Auth;

public class UpdateEmailViewModel
{
    [Required, EmailAddress, StringLength(200)]
    public string NewEmail { get; set; } = string.Empty;
}
