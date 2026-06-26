using System.ComponentModel.DataAnnotations;

namespace GameStore.PL.Pages.Auth
{
    public class ResetPasswordModel : PageModel
    {
        private readonly IAuthService _authService;

        public ResetPasswordModel(IAuthService authService)
        {
            _authService = authService;
        }

        [BindProperty(SupportsGet = true)]
        public string Token { get; set; } = string.Empty;

        [BindProperty]
        [Required, StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = string.Empty;

        [BindProperty]
        [Required, Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;
        public bool IsError { get; set; }
        public bool IsSuccess { get; set; }

        public void OnGet() { }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();
            if (string.IsNullOrEmpty(Token))
            {
                Message = "Invalid reset link.";
                IsError = true;
                return Page();
            }

            var (success, error) = await _authService.ResetPasswordAsync(Token, NewPassword);
            if (success)
            {
                IsSuccess = true;
                Message = "Password reset successfully!";
            }
            else
            {
                Message = error;
                IsError = true;
            }

            return Page();
        }
    }
}
