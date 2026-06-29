using GameStore.PL.Models.Auth;

namespace GameStore.PL.Controllers;

public class ProfileController : Controller
{
    private readonly IAuthService _auth;
    private readonly IUserService _userService;
    private readonly IPostService _postService;
    private readonly IFriendService _friendService;
    private readonly IChatService _chatService;
    private readonly ConnectionTracker _tracker;
    private readonly IDeveloperService _devService;
    private readonly ISaleService _saleService;
    private readonly IMapper _mapper;

    public ProfileController(IAuthService auth, IUserService userService, IPostService postService,
        IFriendService friendService, IChatService chatService, ConnectionTracker tracker,
        IDeveloperService devService, ISaleService saleService, IMapper mapper)
    {
        _auth = auth;
        _userService = userService;
        _postService = postService;
        _friendService = friendService;
        _chatService = chatService;
        _tracker = tracker;
        _devService = devService;
        _saleService = saleService;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return RedirectToAction("Index", "Home");

        var user = await _userService.GetUserByUsernameAsync(username);
        if (user == null)
            return RedirectToAction("Index", "Home");

        var currentUserId = HttpContext.Session.GetString("UserId");
        bool isOwn = currentUserId == user.Id;

        var posts = await _postService.GetUserPostsAsync(user.Id);
        var postCount = await _postService.GetUserPostCountAsync(user.Id);
        bool isFriend = false;
        if (!string.IsNullOrEmpty(currentUserId) && currentUserId != user.Id)
        {
            var friendIds = await _friendService.GetFriendIdsAsync(currentUserId);
            isFriend = friendIds.Contains(user.Id);
        }

        var model = new PublicProfileViewModel
        {
            UserId = user.Id,
            Username = user.Username,
            AvatarUrl = user.AvatarUrl,
            Bio = user.Bio,
            IsOnline = _tracker.IsOnline(user.Id),
            IsOwn = isOwn,
            IsFriend = isFriend,
            PostCount = postCount,
            Posts = _mapper.Map<List<PostViewModel>>(posts)
        };

        var dev = await _devService.GetByUserIdAsync(user.Id);
        if (dev != null)
        {
            model.IsDeveloper = true;
            model.DeveloperId = dev.Id;
            model.DeveloperName = dev.Name;
            model.DeveloperLogoUrl = dev.LogoUrl;
            model.DeveloperDescription = dev.Description;
            model.DeveloperWebsite = dev.Website;
            model.DeveloperCountry = dev.Country;
            var devGames = await _devService.GetGamesAsync(dev.Id);
            model.DevGames = devGames;
            model.DevGameCount = devGames.Count;
            var devGameIds = devGames.Select(g => g.Id).ToList();
            var devActiveSales = await _saleService.GetActiveSalesByGameIdsAsync(devGameIds);
            ViewData["ProfileSales"] = devActiveSales.ToDictionary(s => s.GameId, s => s.NewPrice);
            var stats = await _devService.GetDashboardStatsAsync(dev.Id);
            model.DevTotalDownloads = stats.TotalDownloads;
            model.DevAvgRating = stats.AvgRating;
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit()
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Auth");

        var user = await _userService.GetByIdAsync(userId);
        if (user == null) return RedirectToAction("Login", "Auth");

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
            Posts = _mapper.Map<List<PostViewModel>>(posts),
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
    public async Task<IActionResult> Edit(ProfileViewModel model)
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Auth");

        var user = await _userService.GetByIdAsync(userId);
        if (user == null) return RedirectToAction("Login", "Auth");

        string? avatarUrl = null;
        if (model.AvatarFile is { Length: > 0 })
        {
            var allowedExts = new[] { ".png", ".jpg", ".jpeg", ".gif", ".webp" };
            var ext = Path.GetExtension(model.AvatarFile.FileName).ToLowerInvariant();
            if (!allowedExts.Contains(ext))
            {
                TempData["Message"] = "Avatar must be an image file (PNG, JPG, GIF, WebP).";
                TempData["IsError"] = true;
                return RedirectToAction("Edit");
            }

            if (model.AvatarFile.Length > 5 * 1024 * 1024)
            {
                TempData["Message"] = "Avatar must be under 5 MB.";
                TempData["IsError"] = true;
                return RedirectToAction("Edit");
            }

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
                    return RedirectToAction("Edit");
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
            return RedirectToAction("Edit");
        }

        TempData["Message"] = "Profile updated successfully!";
        return RedirectToAction("Edit");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateEmail(UpdateEmailViewModel model)
    {
        var uid = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(uid)) return RedirectToAction("Login", "Auth");

        if (!ModelState.IsValid)
        {
            TempData["Message"] = "Invalid email address.";
            TempData["IsError"] = true;
            return RedirectToAction("Edit");
        }

        var (success, error) = await _auth.UpdateEmailAsync(uid, model.NewEmail);
        TempData["Message"] = success ? "Email updated successfully!" : error;
        TempData["IsError"] = !success;
        return RedirectToAction("Edit");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        var uid = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(uid)) return RedirectToAction("Login", "Auth");

        if (!ModelState.IsValid)
        {
            TempData["Message"] = "Please fix the validation errors.";
            TempData["IsError"] = true;
            return RedirectToAction("Edit");
        }

        var (success, error) = await _auth.ChangePasswordAsync(uid, model.CurrentPassword, model.NewPassword);
        TempData["Message"] = success ? "Password changed successfully!" : error;
        TempData["IsError"] = !success;
        return RedirectToAction("Edit");
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
}
