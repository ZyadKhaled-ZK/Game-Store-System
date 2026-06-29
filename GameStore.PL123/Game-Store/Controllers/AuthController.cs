using GameStore.PL.Models.Auth;

namespace GameStore.PL.Controllers;

public class AuthController : Controller
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth)
    {
        _auth = auth;
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

        HttpContext.Session.Clear();
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
            HttpContext.Session.Clear();
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

        if (success)
        {
            ViewData["Message"] = "If that email exists, a reset link has been sent.";
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
