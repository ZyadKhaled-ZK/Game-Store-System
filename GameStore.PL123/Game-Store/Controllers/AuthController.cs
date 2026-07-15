using System.Security.Claims;
using Microsoft.AspNetCore.RateLimiting;
using GameStore.PL.Models.Auth;
using GameStore.PL.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;

namespace GameStore.PL.Controllers;

public class AuthController : Controller
{
    private readonly IAuthService _auth;
    private readonly IEmailService _email;

    public AuthController(IAuthService auth, IEmailService email)
    {
        _auth = auth;
        _email = email;
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

        HttpContext.Session.SetString("EmailConfirmed", user.EmailConfirmed.ToString());
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
    [EnableRateLimiting("AuthLogin")]
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

        if (!user.EmailConfirmed)
        {
            ViewData["ErrorMessage"] = "Please verify your email before logging in. Check your inbox for the confirmation link.";
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
    [EnableRateLimiting("AuthRegister")]
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
            var token = await _auth.CreateVerificationTokenAsync(user.Id);
            var confirmLink = Url.Action("VerifyEmail", "Auth", new { token }, Request.Scheme);
            var html = BuildEmailHtml("CONFIRM YOUR EMAIL",
                $"""
                <p style="margin:0 0 16px 0;font-size:14px;line-height:1.6;">Hey {HtmlEncoder(user.Username)}, welcome to <strong>GameStore</strong>!</p>
                <p style="margin:0 0 24px 0;font-size:14px;line-height:1.6;">Click the button below to confirm your email and activate your account:</p>
                <p style="text-align:center;margin:0 0 24px 0;">
                  <a href="{confirmLink}" style="display:inline-block;padding:14px 36px;background:#e63946;color:#fff;font-family:'Share Tech Mono',monospace;font-size:14px;font-weight:700;text-decoration:none;letter-spacing:2px;border-radius:4px;">CONFIRM EMAIL</a>
                </p>
                <p style="margin:0 0 16px 0;font-size:12px;color:#888;line-height:1.6;">If you didn't create this account, you can safely ignore this email.</p>
                """);
            var sent = await _email.SendAsync(user.Email, user.Username, "Welcome to GameStore — Confirm your email", html);
            if (!sent)
                TempData["Message"] = "Account created! Could not send verification email.";
        }

        TempData["EmailSent"] = true;
        return RedirectToAction("VerifyEmailNotice", "Auth");
    }

    [HttpGet]
    [EnableRateLimiting("AuthVerify")]
    public async Task<IActionResult> VerifyEmail(string token)
    {
        if (string.IsNullOrEmpty(token))
            return RedirectToAction("Index", "Home");

        var (success, userId) = await _auth.ConsumeVerificationTokenAsync(token);
        if (!success || userId == null)
        {
            ViewData["Message"] = "Invalid or expired verification link.";
            return View("EmailVerified");
        }

        await _auth.ConfirmEmailAsync(userId);

        ViewData["Message"] = "Email confirmed successfully!";
        return View("EmailVerified");
    }

    [HttpGet]
    public IActionResult VerifyEmailNotice()
    {
        return View();
    }

    [HttpGet]
    [EnableRateLimiting("AuthResend")]
    public async Task<IActionResult> ResendVerification(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            ViewData["ErrorMessage"] = "Invalid email address.";
            return View("Login");
        }

        var existing = await _auth.FindUserByEmailAsync(email);
        if (existing == null || existing.EmailConfirmed)
        {
            ViewData["Message"] = "If that email exists and is unconfirmed, a new verification link has been sent.";
            return View("Login");
        }

        await _auth.InvalidateUserTokensAsync(existing.Id);
        var token = await _auth.CreateVerificationTokenAsync(existing.Id);
        var confirmLink = Url.Action("VerifyEmail", "Auth", new { token }, Request.Scheme);

        var html = BuildEmailHtml("CONFIRM YOUR EMAIL",
            $"""
            <p style="margin:0 0 16px 0;font-size:14px;line-height:1.6;">Hey {HtmlEncoder(existing.Username)}, here's a fresh confirmation link:</p>
            <p style="text-align:center;margin:0 0 24px 0;">
              <a href="{confirmLink}" style="display:inline-block;padding:14px 36px;background:#e63946;color:#fff;font-family:'Share Tech Mono',monospace;font-size:14px;font-weight:700;text-decoration:none;letter-spacing:2px;border-radius:4px;">CONFIRM EMAIL</a>
            </p>
            <p style="margin:0 0 16px 0;font-size:12px;color:#888;line-height:1.6;">If you didn't request this, ignore this email.</p>
            """);
        await _email.SendAsync(existing.Email, existing.Username, "GameStore — Resend: Confirm your email", html);

        TempData["EmailSent"] = true;
        return RedirectToAction("VerifyEmailNotice");
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

        if (success && !string.IsNullOrEmpty(token))
        {
            var resetLink = Url.Action("ResetPassword", "Auth", new { token }, Request.Scheme);
            var html = BuildEmailHtml("RESET YOUR PASSWORD",
                $"""
                <p style="margin:0 0 16px 0;font-size:14px;line-height:1.6;">We received a request to reset your GameStore password.</p>
                <p style="margin:0 0 24px 0;font-size:14px;line-height:1.6;">Click the button below to set a new password. This link expires in <strong>1 hour</strong>.</p>
                <p style="text-align:center;margin:0 0 24px 0;">
                  <a href="{resetLink}" style="display:inline-block;padding:14px 36px;background:#e63946;color:#fff;font-family:'Share Tech Mono',monospace;font-size:14px;font-weight:700;text-decoration:none;letter-spacing:2px;border-radius:4px;">RESET PASSWORD</a>
                </p>
                <p style="margin:0 0 16px 0;font-size:12px;color:#888;line-height:1.6;">If you didn't request this, you can safely ignore this email.</p>
                """);
            var sent = await _email.SendAsync(model.Email, "User", "GameStore — Reset your password", html);
            ViewData["Message"] = sent
                ? "If that email exists, a reset link has been sent."
                : "Could not send reset email. Check server logs for details.";
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

    private static string BuildEmailHtml(string title, string body)
    {
        return $"""
            <!DOCTYPE html>
            <html>
            <head><meta charset="utf-8"></head>
            <body style="margin:0;padding:0;background:#070709;">
              <table role="presentation" style="width:100%;max-width:560px;margin:0 auto;padding:40px 20px;">
                <tr>
                  <td style="text-align:center;padding-bottom:24px;">
                    <span style="font-family:'Orbitron',monospace;font-size:22px;font-weight:900;color:#f5c542;letter-spacing:3px;">GAME<span style="color:#e63946;">STORE</span></span>
                  </td>
                </tr>
                <tr>
                  <td style="background:#0d0d12;border:1px solid rgba(230,57,70,0.3);border-radius:4px;padding:36px;">
                    <h1 style="font-family:'Share Tech Mono',monospace;font-size:16px;font-weight:700;color:#f5c542;letter-spacing:2px;text-align:center;margin:0 0 24px 0;">{title}</h1>
                    <div style="font-family:'Rajdhani',Arial,sans-serif;color:#c0c0c0;">
                      {body}
                    </div>
                    <hr style="border:none;border-top:1px solid rgba(245,197,66,0.15);margin:24px 0 16px 0;">
                    <p style="margin:0;font-size:11px;color:#666;text-align:center;font-family:'Share Tech Mono',monospace;">
                      GameStore &mdash; Your Digital Game Library
                    </p>
                  </td>
                </tr>
              </table>
            </body>
            </html>
            """;
    }

    private static string HtmlEncoder(string value) =>
        System.Net.WebUtility.HtmlEncode(value);
}
