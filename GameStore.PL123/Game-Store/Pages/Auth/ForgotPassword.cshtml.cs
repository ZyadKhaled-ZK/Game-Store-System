using System.ComponentModel.DataAnnotations;

namespace GameStore.PL.Pages.Auth
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly IAuthService _authService;

        public ForgotPasswordModel(IAuthService authService)
        {
            _authService = authService;
        }

        [BindProperty]
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;
        public bool IsError { get; set; }

        public void OnGet() { }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            var (success, error, token) = await _authService.GenerateResetTokenAsync(Email);

            if (success && token != null)
            {
                var resetLink = Url.Page("/Auth/ResetPassword", null, new { token }, Request.Scheme);
                Message = $"If that email exists, a reset link has been sent. For demo: <a href='{resetLink}' style='color:var(--gold);'>Reset Password</a>";
                IsError = false;
            }
            else
            {
                Message = error;
                IsError = false;
            }

            return Page();
        }
    }
}
