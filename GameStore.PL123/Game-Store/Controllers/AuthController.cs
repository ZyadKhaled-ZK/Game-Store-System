using GameStore.PL.Models.Auth;

namespace GameStore.PL.Controllers;

public class AuthController : Controller
{
    private readonly IAuthService _auth;
    private readonly IUserService _userService;

    public AuthController(IAuthService auth, IUserService userService)
    {
        _auth = auth;
        _userService = userService;
    }

    [HttpGet]
    public IActionResult Login()
    {
        var role = HttpContext.Session.GetString("Role");
        if (role == "ADMIN") return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
        if (role == "DEVELOPER") return RedirectToAction("Index", "Dashboard", new { area = "Developer" });

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _auth.LoginAsync(model.Email, model.Password);
        if (user == null)
        {
            ViewData["ErrorMessage"] = "Invalid email or password.";
            return View(model);
        }

        HttpContext.Session.SetString("UserId", user.Id);
        HttpContext.Session.SetString("Username", user.Username);
        HttpContext.Session.SetString("Role", user.Role.ToString());

        if (user.Role == Role.ADMIN)
            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });

        if (user.Role == Role.DEVELOPER)
            return RedirectToAction("Index", "Dashboard", new { area = "Developer" });

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (!string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
            return RedirectToAction("Index", "Home");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var (success, error) = await _auth.RegisterAsync(model.Username, model.Email, model.Password, Role.CUSTOMER);
        if (!success)
        {
            ViewData["Message"] = error;
            ViewData["IsError"] = true;
            return View(model);
        }

        var user = await _auth.LoginAsync(model.Email, model.Password);
        if (user != null)
        {
            HttpContext.Session.SetString("UserId", user.Id);
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("Role", user.Role.ToString());
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Login");
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login");

        var user = await _userService.GetByIdAsync(userId);
        if (user == null) return RedirectToAction("Login");

        var model = new ProfileViewModel
        {
            Username = user.Username,
            Email = user.Email,
            NewEmail = user.Email
        };

        if (TempData.TryGetValue("Message", out var msg)) model.Message = msg?.ToString() ?? "";
        if (TempData.TryGetValue("IsError", out var err)) model.IsError = err is bool b && b;

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateEmail(UpdateEmailViewModel model)
    {
        var uid = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(uid)) return RedirectToAction("Login");

        if (!ModelState.IsValid)
        {
            TempData["Message"] = "Invalid email address.";
            TempData["IsError"] = true;
            return RedirectToAction("Profile");
        }

        var (success, error) = await _auth.UpdateEmailAsync(uid, model.NewEmail);
        TempData["Message"] = success ? "Email updated successfully!" : error;
        TempData["IsError"] = !success;
        return RedirectToAction("Profile");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        var uid = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(uid)) return RedirectToAction("Login");

        if (!ModelState.IsValid)
        {
            TempData["Message"] = "Please fix the validation errors.";
            TempData["IsError"] = true;
            return RedirectToAction("Profile");
        }

        var (success, error) = await _auth.ChangePasswordAsync(uid, model.CurrentPassword, model.NewPassword);
        TempData["Message"] = success ? "Password changed successfully!" : error;
        TempData["IsError"] = !success;
        return RedirectToAction("Profile");
    }

    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var (success, error, token) = await _auth.GenerateResetTokenAsync(model.Email);

        if (success && token != null)
        {
            var resetLink = Url.Action("ResetPassword", "Auth", new { token }, Request.Scheme);
            ViewData["Message"] = $"If that email exists, a reset link has been sent. For demo: <a href='{resetLink}' style='color:var(--gold);'>Reset Password</a>";
        }
        else
        {
            ViewData["Message"] = error;
        }

        ViewData["IsError"] = false;
        return View(model);
    }

    [HttpGet]
    public IActionResult ResetPassword(string token)
    {
        var model = new ResetPasswordViewModel { Token = token ?? "" };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        if (string.IsNullOrEmpty(model.Token))
        {
            ViewData["Message"] = "Invalid reset link.";
            ViewData["IsError"] = true;
            return View(model);
        }

        var (success, error) = await _auth.ResetPasswordAsync(model.Token, model.NewPassword);
        if (success)
        {
            ViewData["IsSuccess"] = true;
            ViewData["Message"] = "Password reset successfully!";
        }
        else
        {
            ViewData["Message"] = error;
            ViewData["IsError"] = true;
        }

        return View(model);
    }
}
