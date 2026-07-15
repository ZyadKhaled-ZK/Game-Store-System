using GameStore.PL.Models.Library;
using GameStore.DAL.Enum;

namespace GameStore.PL.Controllers;

public class LibraryController : Controller
{
    private readonly ILibraryService _libraryService;
    private readonly IGameService _gameService;
    private readonly IGameAccessService _gameAccess;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<LibraryController> _logger;
    private readonly IGameVersionService _gameVersionService;
    private readonly ICreditService _creditService;

    public LibraryController(ILibraryService libraryService, IGameService gameService,
        IGameAccessService gameAccess, IWebHostEnvironment env,
        ILogger<LibraryController> logger,
        IGameVersionService gameVersionService,
        ICreditService creditService)
    {
        _libraryService = libraryService;
        _gameService = gameService;
        _gameAccess = gameAccess;
        _env = env;
        _logger = logger;
        _gameVersionService = gameVersionService;
        _creditService = creditService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var model = new LibraryViewModel();
        var userId = HttpContext.Session.GetString("UserId");
        var roleStr = HttpContext.Session.GetString("Role");
        model.CurrentUserId = userId;
        model.CurrentUserRole = roleStr;

        if (!string.IsNullOrEmpty(userId))
        {
            model.LibraryGames = await _libraryService.GetLibraryGamesAsync(userId);

            if (!string.IsNullOrEmpty(roleStr))
            {
                var role = Enum.Parse<Role>(roleStr);
                model.PreviewableGameIds = await _gameAccess.GetPreviewableGameIdsAsync(userId, role);
            }

            model.AvailableCredit = await _creditService.GetAvailableBalanceAsync(userId);

            var versionTasks = model.LibraryGames
                .Select(lg => _gameVersionService.GetAllAsync(lg.GameId)
                    .ContinueWith(t => (lg.GameId, Versions: t.Result)))
                .ToArray();
            var versionResults = await Task.WhenAll(versionTasks);
            foreach (var (gameId, versions) in versionResults)
                if (versions.Count > 0)
                    model.GameVersions[gameId] = versions;
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Download(string id, string? versionId = null)
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Auth");

        var game = await _gameService.GetByIdAsync(id);
        if (game == null)
            return NotFound();

        if (_gameAccess.IsPreRelease(game))
        {
            var roleStr = HttpContext.Session.GetString("Role") ?? "CUSTOMER";
            var role = Enum.Parse<Role>(roleStr);
            if (!await _gameAccess.CanAccessPreRelease(game, userId, role))
            {
                _logger.LogWarning("Pre-release download denied: user={UserId} game={GameId}", userId, id);
                return Forbid();
            }
        }

        var owns = await _libraryService.HasGame(userId, id);
        if (!owns)
            return Unauthorized();

        // Check version manifest first, fall back to GameFileUrl
        GameVersionModel? version;
        if (!string.IsNullOrEmpty(versionId))
        {
            var all = await _gameVersionService.GetAllAsync(id);
            version = all.FirstOrDefault(v => v.Id == versionId);
        }
        else
        {
            version = await _gameVersionService.GetLatestAsync(id);
        }

        string filePath;
        string downloadName;
        if (version != null)
        {
            var vPath = _gameVersionService.GetFilePath(id, version);
            if (vPath == null || !System.IO.File.Exists(vPath))
                return NotFound();
            filePath = vPath;
            downloadName = version.FileName;
        }
        else if (!string.IsNullOrEmpty(game.GameFileUrl))
        {
            filePath = Path.Combine(_env.WebRootPath, game.GameFileUrl.TrimStart('/'));
            if (!System.IO.File.Exists(filePath))
                return NotFound();
            downloadName = game.GameFileName ?? $"{game.Title}{Path.GetExtension(game.GameFileUrl ?? ".zip")}";
        }
        else
        {
            return NotFound();
        }

        return PhysicalFile(filePath, "application/octet-stream", downloadName);
    }

    [HttpGet]
    public async Task<IActionResult> PreviewDownload(string id, string? versionId = null)
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Auth");

        var game = await _gameService.GetByIdAsync(id);
        if (game == null)
            return NotFound();

        var roleStr = HttpContext.Session.GetString("Role") ?? "CUSTOMER";
        var role = Enum.Parse<Role>(roleStr);

        if (!await _gameAccess.CanAccessPreRelease(game, userId, role))
        {
            _logger.LogError("Preview download denied (possible probing): user={UserId} game={GameId}", userId, id);
            return Forbid();
        }

        if (!_gameAccess.IsPreRelease(game))
            return RedirectToAction("Download", new { id, versionId });

        // Check version manifest first, fall back to GameFileUrl
        GameVersionModel? version;
        if (!string.IsNullOrEmpty(versionId))
        {
            var all = await _gameVersionService.GetAllAsync(id);
            version = all.FirstOrDefault(v => v.Id == versionId);
        }
        else
        {
            version = await _gameVersionService.GetLatestAsync(id);
        }

        string filePath;
        string downloadName;
        if (version != null)
        {
            var vPath = _gameVersionService.GetFilePath(id, version);
            if (vPath == null || !System.IO.File.Exists(vPath))
                return NotFound();
            filePath = vPath;
            downloadName = version.FileName;
        }
        else if (!string.IsNullOrEmpty(game.GameFileUrl))
        {
            filePath = Path.Combine(_env.WebRootPath, game.GameFileUrl.TrimStart('/'));
            if (!System.IO.File.Exists(filePath))
                return NotFound();
            downloadName = game.GameFileName ?? $"{game.Title}{Path.GetExtension(game.GameFileUrl ?? ".zip")}";
        }
        else
        {
            return NotFound();
        }

        return PhysicalFile(filePath, "application/octet-stream", downloadName);
    }
}
