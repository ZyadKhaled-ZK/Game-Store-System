using GameStore.PL.Models.Auth;

namespace GameStore.PL.Controllers;

public class AuthController : Controller
{
    private readonly IAuthService _auth;
    private readonly IUserService _userService;
    private readonly IPostService _postService;
    private readonly IChatService _chatService;
    private readonly ConnectionTracker _tracker;

    public AuthController(IAuthService auth, IUserService userService, IPostService postService,
        IChatService chatService, ConnectionTracker tracker)
    {
        _auth = auth;
        _userService = userService;
        _postService = postService;
        _chatService = chatService;
        _tracker = tracker;
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
    public async Task<IActionResult> Profile()
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login");

        var user = await _userService.GetByIdAsync(userId);
        if (user == null) return RedirectToAction("Login");

        var posts = await _postService.GetUserPostsAsync(userId);
        var postCount = await _postService.GetUserPostCountAsync(userId);
        var conversations = await _chatService.GetConversationsAsync(userId);
        var convUserIds = conversations.Select(c => c.UserId).ToList();
        var convUsers = convUserIds.Any()
            ? await _userService.GetUsersByIdsAsync(convUserIds)
            : new List<User>();

        var model = new ProfileViewModel
        {
            Username = user.Username,
            Email = user.Email,
            NewEmail = user.Email,
            AvatarUrl = user.AvatarUrl,
            Bio = user.Bio,
            Posts = posts.Select(p => new PostViewModel
            {
                Id = p.Id,
                Content = p.Content,
                CreatedAt = p.CreatedAt
            }).ToList(),
            PostCount = postCount,
            RecentConversations = conversations.Select(c => new ConversationPreviewViewModel
            {
                UserId = c.UserId,
                Username = c.Username,
                Avatar = convUsers.FirstOrDefault(u => u.Id == c.UserId)?.AvatarUrl,
                LastMessage = c.LastMessage?.Content,
                LastMessageAt = c.LastMessage?.SentAt,
                UnreadCount = c.UnreadCount,
                IsOnline = _tracker.IsOnline(c.UserId)
            }).ToList()
        };

        if (TempData.TryGetValue("Message", out var msg)) model.Message = msg?.ToString() ?? "";
        if (TempData.TryGetValue("IsError", out var err)) model.IsError = err is bool b && b;

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login");

        var user = await _userService.GetByIdAsync(userId);
        if (user == null) return RedirectToAction("Login");

        string? avatarUrl = null;
        if (model.AvatarFile is { Length: > 0 })
        {
            var allowedExts = new[] { ".png", ".jpg", ".jpeg", ".gif", ".webp" };
            var ext = Path.GetExtension(model.AvatarFile.FileName).ToLowerInvariant();
            if (!allowedExts.Contains(ext))
            {
                TempData["Message"] = "Avatar must be an image file (PNG, JPG, GIF, WebP).";
                TempData["IsError"] = true;
                return RedirectToAction("Profile");
            }

            if (model.AvatarFile.Length > 5 * 1024 * 1024)
            {
                TempData["Message"] = "Avatar must be under 5 MB.";
                TempData["IsError"] = true;
                return RedirectToAction("Profile");
            }

            // Validate magic bytes
            var headerBytes = new byte[8];
            using (var ms = new MemoryStream())
            {
                await model.AvatarFile.CopyToAsync(ms);
                var allBytes = ms.ToArray();
                Array.Copy(allBytes, headerBytes, Math.Min(8, allBytes.Length));
                var allowedSignatures = new Dictionary<string, byte[]>
                {
                    { ".png", [0x89, 0x50, 0x4E, 0x47] },
                    { ".jpg", [0xFF, 0xD8] },
                    { ".jpeg", [0xFF, 0xD8] },
                    { ".gif", [0x47, 0x49, 0x46] },
                    { ".webp", [0x52, 0x49, 0x46, 0x46] },
                };
                var sig = allowedSignatures[ext];
                var match = headerBytes.Take(sig.Length).SequenceEqual(sig) ||
                            (ext is ".jpg" or ".jpeg" && headerBytes[0] == 0xFF && headerBytes[1] == 0xD8);
                if (!match)
                {
                    TempData["Message"] = "Avatar file content does not match the expected image type.";
                    TempData["IsError"] = true;
                    return RedirectToAction("Profile");
                }

                var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "avatars");
                Directory.CreateDirectory(uploadsDir);

                var fileName = $"{Guid.NewGuid():N}{ext}";
                var filePath = Path.Combine(uploadsDir, fileName);
                await System.IO.File.WriteAllBytesAsync(filePath, allBytes);
                avatarUrl = $"/uploads/avatars/{fileName}";
            }
        }

        var (success, error) = await _userService.UpdateProfileAsync(userId, avatarUrl, model.Bio);
        if (!success)
        {
            TempData["Message"] = error;
            TempData["IsError"] = true;
            return RedirectToAction("Profile");
        }

        TempData["Message"] = "Profile updated successfully!";
        return RedirectToAction("Profile");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePost(string content)
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId))
            return Json(new { success = false, message = "Please login first." });

        if (string.IsNullOrWhiteSpace(content))
            return Json(new { success = false, message = "Post content is required." });

        var post = await _postService.CreateAsync(userId, content);
        return Json(new { success = true, id = post.Id, createdAt = post.CreatedAt.ToString("MMM dd, yyyy HH:mm") });
    }

    [HttpGet]
    public async Task<IActionResult> GetAvatarUrl()
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId))
            return Json(new { url = (string?)null, initial = "?" });

        var user = await _userService.GetByIdAsync(userId);
        if (user == null)
            return Json(new { url = (string?)null, initial = "?" });

        return Json(new { url = user.AvatarUrl, initial = user.Username?.Substring(0, 1)?.ToUpper() ?? "?" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePost(string postId)
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId))
            return Json(new { success = false, message = "Please login first." });

        var ok = await _postService.DeleteAsync(postId, userId);
        return Json(new { success = ok, message = ok ? "Post deleted." : "Post not found or not yours." });
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
