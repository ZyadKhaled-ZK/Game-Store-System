using GameStore.PL.Models.Library;

namespace GameStore.PL.Controllers;

public class LibraryController : Controller
{
    private readonly ILibraryService _libraryService;

    public LibraryController(ILibraryService libraryService)
    {
        _libraryService = libraryService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var model = new LibraryViewModel();
        var userId = HttpContext.Session.GetString("UserId");
        if (!string.IsNullOrEmpty(userId))
            model.LibraryGames = await _libraryService.GetLibraryGamesAsync(userId);

        return View(model);
    }
}
