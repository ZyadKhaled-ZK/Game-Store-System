using System.ComponentModel.DataAnnotations;

namespace GameStore.PL.Pages.Auth
{
    public class RegisterModel : PageModel
    {
        private readonly IAuthService _auth;

        public RegisterModel(IAuthService auth) => _auth = auth;

        [BindProperty, Required, StringLength(100, MinimumLength = 3)]
        public string Username { get; set; } = string.Empty;

        [BindProperty, Required, EmailAddress, StringLength(200)]
        public string Email    { get; set; } = string.Empty;

        [BindProperty, Required, StringLength(100, MinimumLength = 6), DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool   IsError { get; set; }

        public IActionResult OnGet()
        {
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
                return RedirectToPage("/Index");
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            var (success, error) = await _auth.RegisterAsync(Username, Email, Password, GameStore.DAL.Enum.Role.CUSTOMER);
            if (!success)
            {
                IsError = true;
                Message = error;
                return Page();
            }

            var user = await _auth.LoginAsync(Email, Password);
            if (user != null)
            {
                HttpContext.Session.SetString("UserId", user.Id);
                HttpContext.Session.SetString("Username", user.Username);
                HttpContext.Session.SetString("Role", user.Role.ToString());
            }

            return RedirectToPage("/Index");
        }
    }
}
