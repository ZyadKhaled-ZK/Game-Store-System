using System.ComponentModel.DataAnnotations;

namespace GameStore.PL.Pages.Auth
{
    public class ProfileModel : PageModel
    {
        private readonly IAuthService _auth;
        private readonly IUserService _userService;

        public ProfileModel(IAuthService auth, IUserService userService)
        {
            _auth = auth;
            _userService = userService;
        }

        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsError { get; set; }

        [BindProperty, Required, EmailAddress, StringLength(200)]
        public string NewEmail { get; set; } = string.Empty;

        [BindProperty, Required, DataType(DataType.Password)]
        public string CurrentPassword { get; set; } = string.Empty;

        [BindProperty, Required, StringLength(100, MinimumLength = 6), DataType(DataType.Password)]
        public string NewPassword { get; set; } = string.Empty;

        [BindProperty, Required, DataType(DataType.Password), Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        public async Task<IActionResult> OnGet()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
                return RedirectToPage("/Auth/Login");

            var user = await _userService.GetByIdAsync(userId);
            if (user == null) return RedirectToPage("/Auth/Login");

            Username = user.Username;
            Email = user.Email;
            NewEmail = user.Email;

            return Page();
        }

        public async Task<IActionResult> OnPostEmailAsync()
        {
            if (!ModelState.IsValid) return Page();

            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId)) return RedirectToPage("/Auth/Login");

            var (success, error) = await _auth.UpdateEmailAsync(userId, NewEmail);
            if (!success)
            {
                Message = error;
                IsError = true;
            }
            else
            {
                Message = "Email updated successfully!";
                IsError = false;
                Email = NewEmail;
            }

            var user = await _userService.GetByIdAsync(userId);
            if (user != null) Username = user.Username;

            return Page();
        }

        public async Task<IActionResult> OnPostPasswordAsync()
        {
            if (!ModelState.IsValid) return Page();

            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId)) return RedirectToPage("/Auth/Login");

            var (success, error) = await _auth.ChangePasswordAsync(userId, CurrentPassword, NewPassword);
            if (!success)
            {
                Message = error;
                IsError = true;
            }
            else
            {
                Message = "Password changed successfully!";
                IsError = false;
            }

            var u = await _userService.GetByIdAsync(userId);
            if (u != null) { Username = u.Username; Email = u.Email; NewEmail = u.Email; }

            return Page();
        }
    }
}
