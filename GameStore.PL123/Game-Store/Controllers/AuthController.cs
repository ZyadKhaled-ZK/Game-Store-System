using System.Security.Claims;
using GameStore.PL.Models.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;

namespace GameStore.PL.Controllers;

public class AuthController : Controller
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth)
    {
        _auth = auth;
    }

    private async Task SignInUserAsync(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.Username),
            new("Role", user.Role.ToString())
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(new ClaimsPrincipal(identity));
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
            if (await _auth.IsExternalAccountAsync(model.Email))
            {
                ViewData["GoogleHint"] = true;
                return View(model);
            }
            ViewData["ErrorMessage"] = "Invalid email or password.";
            return View(model);
        }

        HttpContext.Session.Clear();
        HttpContext.Session.SetString("UserId", user.Id);
        HttpContext.Session.SetString("Username", user.Username);
        HttpContext.Session.SetString("Role", user.Role.ToString());

        await SignInUserAsync(user);

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

            await SignInUserAsync(user);
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    [IgnoreAntiforgeryToken]
    public async Task<JsonResult> CheckUsername(string username)
    {
        var exists = await _auth.UsernameExistsAsync(username);
        return Json(!exists);
    }

    [HttpGet]
    public IActionResult ExternalLogin(string provider, string returnUrl = "/")
    {
        if (!string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
            return RedirectToAction("Index", "Home");

        var redirectUrl = Url.Action("ExternalCallback", "Auth", new { returnUrl });
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
        return Challenge(properties, provider);
    }

    [HttpGet]
    public async Task<IActionResult> ExternalCallback(string returnUrl = "/")
    {
        var result = await HttpContext.AuthenticateAsync();
        if (!result.Succeeded)
        {
            ViewData["AuthError"] = "Google authentication failed. Please try again.";
            return RedirectToAction("Login");
        }

        var claims = result.Principal?.Claims;
        var email  = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        var name   = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
        var avatar = claims?.FirstOrDefault(c => c.Type == "picture")?.Value;

        if (string.IsNullOrEmpty(email))
            return RedirectToAction("Login");

        var (user, isNew) = await _auth.GetOrCreateExternalUserAsync(email, name ?? email.Split('@')[0], avatar);
        if (user == null)
            return RedirectToAction("Login");

        HttpContext.Session.Clear();
        HttpContext.Session.SetString("UserId", user.Id);
        HttpContext.Session.SetString("Username", user.Username);
        HttpContext.Session.SetString("Role", user.Role.ToString());

        await SignInUserAsync(user);

        if (isNew)
            return RedirectToAction("CompleteRegistration");

        return Redirect(returnUrl);
    }

    [HttpGet]
    public IActionResult CompleteRegistration()
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login");

        return View(new CompleteRegistrationViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteRegistration(CompleteRegistrationViewModel model)
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login");

        if (!ModelState.IsValid) return View(model);

        var (success, error) = await _auth.UpdateProfileAsync(userId, model.Username, model.Bio);
        if (!success)
        {
            ViewData["Message"] = error;
            ViewData["IsError"] = true;
            return View(model);
        }

        HttpContext.Session.SetString("Username", model.Username);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, model.Username),
            new("Role", HttpContext.Session.GetString("Role") ?? "CUSTOMER")
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(new ClaimsPrincipal(identity));

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public async Task<IActionResult> Logout()
    {
        HttpContext.Session.Clear();
        await HttpContext.SignOutAsync();
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
