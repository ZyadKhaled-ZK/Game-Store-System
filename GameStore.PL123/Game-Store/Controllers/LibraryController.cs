using GameStore.PL.Models.Library;

namespace GameStore.PL.Controllers;

public class LibraryController : Controller
{
    private readonly ILibraryService _libraryService;
    private readonly IGameService _gameService;
    private readonly IWebHostEnvironment _env;

    public LibraryController(ILibraryService libraryService, IGameService gameService,
        IWebHostEnvironment env)
    {
        _libraryService = libraryService;
        _gameService = gameService;
        _env = env;
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

    [HttpGet]
    public async Task<IActionResult> Download(string id)
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Auth");

        var game = await _gameService.GetByIdAsync(id);
        if (game == null || string.IsNullOrEmpty(game.GameFileUrl))
            return NotFound();

        var owns = await _libraryService.HasGame(userId, id);
        if (!owns)
            return Unauthorized();

        var filePath = Path.Combine(_env.WebRootPath, game.GameFileUrl.TrimStart('/'));
        if (!System.IO.File.Exists(filePath))
            return NotFound();

        return PhysicalFile(filePath, "application/octet-stream", game.GameFileName ?? "game.zip");
    }
}
