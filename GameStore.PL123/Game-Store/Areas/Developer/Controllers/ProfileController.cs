using System.ComponentModel.DataAnnotations;
using GameStore.PL.Filters;

namespace GameStore.PL.Areas.Developer.Controllers;

[Area("Developer")]
[ServiceFilter(typeof(DeveloperOnlyFilter))]
public class ProfileController : Controller
{
    private readonly IDeveloperService _devService;

    public ProfileController(IDeveloperService devService)
    {
        _devService = devService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Auth");

        ViewData["Title"] = "Profile";
        var dev = await _devService.GetByUserIdAsync(userId);
        return View(dev);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(
        [Required, StringLength(200)] string Name,
        string? Slug,
        [StringLength(2000)] string? Description,
        [StringLength(500)] string? Website,
        [StringLength(500)] string? LogoUrl,
        [StringLength(100)] string? Country)
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Auth");

        if (!ModelState.IsValid)
        {
            TempData["Message"] = "Name is required.";
            TempData["IsError"] = true;
            return RedirectToAction("Index");
        }

        var (success, error) = await _devService.CreateOrUpdateProfileAsync(userId, Name, Slug, Description, Website, LogoUrl, Country);
        TempData["Message"] = success ? "Profile saved." : error;
        TempData["IsError"] = !success;
        return RedirectToAction("Index");
    }
}
