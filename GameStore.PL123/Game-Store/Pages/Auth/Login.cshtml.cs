using System.ComponentModel.DataAnnotations;

namespace GameStore.PL.Pages.Auth
{
    public class LoginModel : PageModel
    {
        private readonly IAuthService _auth;

        public LoginModel(IAuthService auth) => _auth = auth;

        [BindProperty, Required, EmailAddress, StringLength(200)]
        public string Email { get; set; } = string.Empty;

        [BindProperty, Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;

        public IActionResult OnGet()
        {
            var role = HttpContext.Session.GetString("Role");
            if (role == "ADMIN") return RedirectToPage("/Admin/Dashboard");

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            var user = await _auth.LoginAsync(Email, Password);
            if (user == null)
            {
                ErrorMessage = "Invalid email or password.";
                return Page();
            }

            HttpContext.Session.SetString("UserId", user.Id);
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("Role", user.Role.ToString());

            if (user.Role == GameStore.DAL.Enum.Role.ADMIN)
                return RedirectToPage("/Admin/Dashboard");

            return RedirectToPage("/", new { welcome = true });
        }
    }
}
