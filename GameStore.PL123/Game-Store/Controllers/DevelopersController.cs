namespace GameStore.PL.Controllers;

public class DevelopersController : Controller
{
    private readonly IDeveloperService _devService;

    public DevelopersController(IDeveloperService devService)
    {
        _devService = devService;
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

        ViewData["Title"] = dev.Name;
        var games = await _devService.GetGamesAsync(id);
        var stats = await _devService.GetDashboardStatsAsync(id);

        ViewData["GameCount"] = stats.GameCount;
        ViewData["TotalDownloads"] = stats.TotalDownloads;
        ViewData["AvgRating"] = stats.AvgRating;

        return View(Tuple.Create(dev, games));
    }
}
