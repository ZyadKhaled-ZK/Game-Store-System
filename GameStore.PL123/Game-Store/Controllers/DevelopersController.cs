namespace GameStore.PL.Controllers;

public class DevelopersController : Controller
{
    private readonly IDeveloperService _devService;
    private readonly IUserService _userService;

    public DevelopersController(IDeveloperService devService, IUserService userService)
    {
        _devService = devService;
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Developers";
        var developers = await _devService.GetAllAsync();
        return View(developers);
    }

    [HttpGet]
    public async Task<IActionResult> Details(string id)
    {
        var dev = await _devService.GetByIdAsync(id);
        if (dev == null) return NotFound();

        var user = await _userService.GetByIdAsync(dev.UserId);
        if (user == null) return NotFound();

        return RedirectToAction("Index", "Profile", new { username = user.Username });
    }
}
