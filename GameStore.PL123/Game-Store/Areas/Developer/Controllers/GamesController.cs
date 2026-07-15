using System.ComponentModel.DataAnnotations;
using GameStore.PL.Filters;

namespace GameStore.PL.Areas.Developer.Controllers;

[Area("Developer")]
[ServiceFilter(typeof(DeveloperOnlyFilter))]
public class GamesController : Controller
{
    private readonly IDeveloperService _devService;
    private readonly IGameService _gameService;
    private readonly ICategoryService _categoryService;
    private readonly IGameFileService _gameFileService;
    private readonly ILibraryService _libraryService;
    private readonly IWebHostEnvironment _env;
    private readonly ISystemRequirementService _systemReqService;
    private readonly IGameVersionService _gameVersionService;

    public GamesController(IDeveloperService devService, IGameService gameService,
        ICategoryService categoryService, IGameFileService gameFileService,
        ILibraryService libraryService, IWebHostEnvironment env,
        ISystemRequirementService systemReqService,
        IGameVersionService gameVersionService)
    {
        _devService = devService;
        _gameService = gameService;
        _categoryService = categoryService;
        _gameFileService = gameFileService;
        _libraryService = libraryService;
        _env = env;
        _systemReqService = systemReqService;
        _gameVersionService = gameVersionService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1)
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Auth");

        var dev = await _devService.GetByUserIdAsync(userId);
        if (dev == null)
        {
            TempData["Message"] = "Create your developer profile first.";
            TempData["IsError"] = true;
            return RedirectToAction("Index", "Profile");
        }

        ViewData["Title"] = "My Games";

        if (TempData.TryGetValue("Message", out var msg)) ViewData["Message"] = msg;
        if (TempData.TryGetValue("IsError", out var err)) ViewData["IsError"] = err is bool b && b;

        var result = await _devService.GetGamesAsync(dev.Id, page);
        ViewData["Categories"] = await _categoryService.GetAllAsync();
        ViewData["DeveloperId"] = dev.Id;

        return View(result);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Required, StringLength(200)] string Title,
        [StringLength(4000)] string? Description,
        [Range(0, 9999.99)] decimal Price,
        [Required] DateTime ReleaseDate,
        [StringLength(500)] string? CoverImageUrl,
        [StringLength(500)] string? TrailerUrl,
        List<string>? CategoryIds,
        IFormFile? GameFile,
        IFormFile? CoverImageFile)
    {
        if (!ModelState.IsValid)
        {
            TempData["Message"] = "Please fill in all required fields.";
            TempData["IsError"] = true;
            return RedirectToAction("Index");
        }

        var userId = HttpContext.Session.GetString("UserId");
        var dev = await _devService.GetByUserIdAsync(userId!);

        var game = new Game
        {
            Title = Title,
            Description = Description,
            Price = Price,
            ReleaseDate = ReleaseDate,
            CoverImageUrl = CoverImageUrl,
            TrailerUrl = TrailerUrl,
            Developer = dev?.Name,
            DeveloperId = dev?.Id
        };

        if (CoverImageFile != null)
            game.CoverImageUrl = await SaveCoverImageFileAsync(game.Id, CoverImageFile);

        await _gameService.CreateAsync(game, CategoryIds ?? new());
        await _libraryService.AddGameToLibraryAsync(userId!, game.Id);

        if (GameFile != null)
        {
            var (fileUrl, fileName, fileSize) = await SaveGameFileAsync(game.Id, GameFile);
            await _gameFileService.UpdateGameFileAsync(game.Id, fileUrl, fileName, fileSize);
        }

        TempData["Message"] = $"Game '{Title}' published.";
        TempData["IsError"] = false;
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        [Required] string GameId,
        [Required, StringLength(200)] string Title,
        [StringLength(4000)] string? Description,
        [Range(0, 9999.99)] decimal Price,
        [Required] DateTime ReleaseDate,
        [StringLength(500)] string? CoverImageUrl,
        [StringLength(500)] string? TrailerUrl,
        List<string>? CategoryIds,
        IFormFile? GameFile, bool RemoveGameFile = false,
        IFormFile? CoverImageFile = null)
    {
        if (!ModelState.IsValid) return RedirectToAction("Index");

        var userId = HttpContext.Session.GetString("UserId");
        var dev = await _devService.GetByUserIdAsync(userId!);
        if (dev == null)
        {
            TempData["Message"] = "Create your developer profile first.";
            TempData["IsError"] = true;
            return RedirectToAction("Index", "Profile");
        }

        var oldGame = await _gameService.GetByIdAsync(GameId);
        if (oldGame == null || oldGame.DeveloperId != dev.Id)
        {
            TempData["Message"] = "Game not found or access denied.";
            TempData["IsError"] = true;
            return RedirectToAction("Index");
        }

        var update = new Game
        {
            Title = Title,
            Description = Description,
            Price = Price,
            ReleaseDate = ReleaseDate,
            CoverImageUrl = CoverImageFile != null ? await SaveCoverImageFileAsync(GameId, CoverImageFile) : CoverImageUrl,
            TrailerUrl = TrailerUrl,
            Developer = dev.Name,
            DeveloperId = dev.Id
        };

        await _gameService.UpdateAsync(GameId, update, CategoryIds ?? new());

        if (CoverImageFile != null && oldGame.CoverImageUrl != null)
            DeleteCoverImageFile(oldGame.CoverImageUrl);

        if (RemoveGameFile)
        {
            await DeleteGameFileAsync(GameId);
            await _gameFileService.ClearGameFileAsync(GameId);
        }

        if (GameFile != null)
        {
            await DeleteGameFileAsync(GameId);
            var (fileUrl, fileName, fileSize) = await SaveGameFileAsync(GameId, GameFile);
            await _gameFileService.UpdateGameFileAsync(GameId, fileUrl, fileName, fileSize);
        }

        TempData["Message"] = $"Game '{Title}' updated.";
        TempData["IsError"] = false;
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete([Required] string id)
    {
        if (!ModelState.IsValid) return RedirectToAction("Index");

        var userId = HttpContext.Session.GetString("UserId");
        var dev = await _devService.GetByUserIdAsync(userId!);

        var game = await _gameService.GetByIdAsync(id);
        if (game == null || game.DeveloperId != dev?.Id)
        {
            TempData["Message"] = "Game not found or access denied.";
            TempData["IsError"] = true;
            return RedirectToAction("Index");
        }

        await DeleteGameFolderAsync(id);
        await _gameService.DeleteAsync(id);
        TempData["Message"] = "Game deleted.";
        TempData["IsError"] = false;
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveRequirements([FromBody] Admin.Controllers.GamesController.SaveRequirementsRequest request)
    {
        var userId = HttpContext.Session.GetString("UserId");
        var dev = await _devService.GetByUserIdAsync(userId!);
        var game = await _gameService.GetByIdAsync(request.GameId);
        if (game == null || game.DeveloperId != dev?.Id)
            return Json(new { success = false, message = "Access denied." });
        if (!ModelState.IsValid) return Json(new { success = false, message = "Invalid data." });
        await _systemReqService.SaveAsync(request.GameId, request.Model);
        return Json(new { success = true });
    }

    [HttpGet]
    public async Task<IActionResult> GetRequirements(string id)
    {
        var reqs = await _systemReqService.GetAsync(id);
        return Json(reqs ?? new SystemRequirementsModel());
    }

    // ── Game Version Endpoints ───────────────────────────────────────────

    private async Task<bool> OwnsGame(string gameId)
    {
        var userId = HttpContext.Session.GetString("UserId");
        var dev = await _devService.GetByUserIdAsync(userId!);
        var game = await _gameService.GetByIdAsync(gameId);
        return game != null && game.DeveloperId == dev?.Id;
    }

    [HttpGet]
    public async Task<IActionResult> GetVersions(string id)
    {
        if (!await OwnsGame(id)) return Json(new { success = false, message = "Access denied." });
        var versions = await _gameVersionService.GetAllAsync(id);
        return Json(versions);
    }

    [HttpPost]
    public async Task<IActionResult> UploadVersion(string gameId, string versionLabel, string? changelog, IFormFile file)
    {
        if (!await OwnsGame(gameId)) return Json(new { success = false, message = "Access denied." });
        if (file == null || file.Length == 0)
            return Json(new { success = false, message = "No file uploaded." });
        if (string.IsNullOrWhiteSpace(versionLabel))
            return Json(new { success = false, message = "Version label is required." });

        var (success, message) = await _gameVersionService.CreateAsync(gameId, versionLabel, changelog ?? "", file);
        return Json(new { success, message });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteVersion(string gameId, string versionId)
    {
        if (!await OwnsGame(gameId)) return Json(new { success = false, message = "Access denied." });
        var (success, message) = await _gameVersionService.DeleteAsync(gameId, versionId);
        return Json(new { success, message });
    }

    [HttpPost]
    public async Task<IActionResult> SetCurrentVersion(string gameId, string versionId)
    {
        if (!await OwnsGame(gameId)) return Json(new { success = false, message = "Access denied." });
        var (success, message) = await _gameVersionService.SetCurrentAsync(gameId, versionId);
        return Json(new { success, message });
    }

    private async Task<(string fileUrl, string fileName, long fileSize)> SaveGameFileAsync(string gameId, IFormFile file)
    {
        var folder = Path.Combine(_env.WebRootPath, "uploads", "games", gameId, "files");
        Directory.CreateDirectory(folder);

        var ext = Path.GetExtension(file.FileName);
        var storedName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(folder, storedName);

        await using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);

        var fileUrl = $"/uploads/games/{gameId}/files/{storedName}";
        return (fileUrl, file.FileName, file.Length);
    }

    private Task DeleteGameFileAsync(string gameId)
    {
        var folder = Path.Combine(_env.WebRootPath, "uploads", "games", gameId, "files");
        if (Directory.Exists(folder))
            Directory.Delete(folder, recursive: true);
        return Task.CompletedTask;
    }

    private Task DeleteGameFolderAsync(string gameId)
    {
        var folder = Path.Combine(_env.WebRootPath, "uploads", "games", gameId);
        if (Directory.Exists(folder))
            Directory.Delete(folder, recursive: true);
        return Task.CompletedTask;
    }

    private async Task<string> SaveCoverImageFileAsync(string gameId, IFormFile file)
    {
        var folder = Path.Combine(_env.WebRootPath, "uploads", "games", gameId, "cover");
        Directory.CreateDirectory(folder);

        var ext = Path.GetExtension(file.FileName);
        var storedName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(folder, storedName);

        await using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);

        return $"/uploads/games/{gameId}/cover/{storedName}";
    }

    private void DeleteCoverImageFile(string coverImageUrl)
    {
        if (string.IsNullOrEmpty(coverImageUrl) || !coverImageUrl.StartsWith("/uploads/"))
            return;
        var filePath = Path.Combine(_env.WebRootPath, coverImageUrl.TrimStart('/'));
        if (System.IO.File.Exists(filePath))
            System.IO.File.Delete(filePath);
    }
}
