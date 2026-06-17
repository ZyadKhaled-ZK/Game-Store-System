namespace GameStore.PL.Pages.Auth
{
    public class LoginModel : PageModel
    {
        private readonly AuthService _auth;

        public LoginModel(AuthService auth) => _auth = auth;

        [BindProperty] public string Email { get; set; } = string.Empty;
        [BindProperty] public string Password { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;

        public IActionResult OnGet()
        {
            var role = HttpContext.Session.GetString("Role");
            if (role == "ADMIN") return RedirectToPage("/Admin/Dashboard");

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _auth.LoginAsync(Email, Password);
            if (user == null)
            {
                ErrorMessage = "Invalid email or password.";
                return Page();
            }

            var role = user.Role;
            if (role != GameStore.DAL.Enum.Role.DEVELOPER && role != GameStore.DAL.Enum.Role.ADMIN)
            {
                ErrorMessage = "Access denied. Admin or Developer account required.";
                return Page();
            }

            HttpContext.Session.SetString("UserId", user.Id);
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("Role", user.Role.ToString());

            if (role == GameStore.DAL.Enum.Role.ADMIN)
                return RedirectToPage("/Admin/Dashboard");

            return RedirectToPage("/");
        }
    }
}
