using GameStore.PL.Services;

namespace GameStore.PL.Controllers;

public class BecomeDeveloperController : Controller
{
    private readonly IDeveloperService _devService;
    private readonly IUserService _userService;
    private readonly IDeveloperApplicationService _appService;
    private readonly INotificationService _notifService;

    public BecomeDeveloperController(IDeveloperService devService, IUserService userService, IDeveloperApplicationService appService, INotificationService notifService)
    {
        _devService = devService;
        _userService = userService;
        _appService = appService;
        _notifService = notifService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = HttpContext.Session.GetString("UserId");
        var role = HttpContext.Session.GetString("Role");

        if (string.IsNullOrEmpty(userId))
        {
            ViewData["NotLoggedIn"] = true;
            return View();
        }

        var user = await _userService.GetByIdAsync(userId);
        var dbRole = user?.Role.ToString();
        if (dbRole != null && dbRole != role)
        {
            HttpContext.Session.SetString("Role", dbRole);
            role = dbRole;
        }

        if (role == Role.ADMIN.ToString())
        {
            ViewData["IsAdmin"] = true;
            return View();
        }

        if (role == Role.DEVELOPER.ToString() && await _devService.IsDeveloperUserAsync(userId))
            return RedirectToAction("Index", "Dashboard", new { area = "Developer" });

        if (role == Role.DEVELOPER.ToString())
        {
            ViewData["NoProfile"] = true;
            return View();
        }

        var pending = await _appService.GetByUserIdAsync(userId);
        if (pending != null)
        {
            ViewData["HasPendingApplication"] = true;
            return View();
        }

        if (TempData.TryGetValue("Message", out var msg)) ViewData["Message"] = msg;
        if (TempData.TryGetValue("IsError", out var err)) ViewData["IsError"] = err is bool b && b;

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(string name, string? description, string? website, string? country, string? githubUrl, IFormFile? cvFile)
    {
        var userId = HttpContext.Session.GetString("UserId");
        var role = HttpContext.Session.GetString("Role");

        if (string.IsNullOrEmpty(userId))
        {
            ViewData["NotLoggedIn"] = true;
            return View();
        }

        if (role == Role.ADMIN.ToString())
        {
            ViewData["IsAdmin"] = true;
            return View();
        }

        if (role == Role.DEVELOPER.ToString() && await _devService.IsDeveloperUserAsync(userId))
            return RedirectToAction("Index", "Dashboard", new { area = "Developer" });

        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Message"] = "Studio name is required.";
            TempData["IsError"] = true;
            return RedirectToAction("Index");
        }

        string? cvPath = null;
        if (cvFile is { Length: > 0 })
        {
            var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "cvs");
            Directory.CreateDirectory(uploadsDir);

            var ext = Path.GetExtension(cvFile.FileName).ToLowerInvariant();
            if (ext != ".pdf" && ext != ".doc" && ext != ".docx")
            {
                TempData["Message"] = "CV must be a PDF or Word document.";
                TempData["IsError"] = true;
                return RedirectToAction("Index");
            }

            var fileName = $"{Guid.NewGuid():N}{ext}";
            var filePath = Path.Combine(uploadsDir, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await cvFile.CopyToAsync(stream);
            }
            cvPath = $"/uploads/cvs/{fileName}";
        }

        var (success, error) = await _appService.SubmitAsync(userId, name, description, website, country, cvPath, githubUrl);
        if (!success)
        {
            TempData["Message"] = error;
            TempData["IsError"] = true;
            return RedirectToAction("Index");
        }

        var username = HttpContext.Session.GetString("Username") ?? "A user";
        await _notifService.SendToAdminsAsync(
            "New Developer Application",
            $"{username} has applied to become a developer (\"{name}\").",
            "info");

        TempData["Message"] = "Your application has been submitted! An admin will review it shortly.";
        return RedirectToAction("Index");
    }
}
