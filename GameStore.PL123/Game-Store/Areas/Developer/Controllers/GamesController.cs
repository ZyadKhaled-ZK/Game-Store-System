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

    public GamesController(IDeveloperService devService, IGameService gameService,
        ICategoryService categoryService, IGameFileService gameFileService,
        ILibraryService libraryService, IWebHostEnvironment env)
    {
        _devService = devService;
        _gameService = gameService;
        _categoryService = categoryService;
        _gameFileService = gameFileService;
        _libraryService = libraryService;
        _env = env;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
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

        var model = new List<Game>();
        if (TempData.TryGetValue("Message", out var msg)) ViewData["Message"] = msg;
        if (TempData.TryGetValue("IsError", out var err)) ViewData["IsError"] = err is bool b && b;

        model = await _devService.GetGamesAsync(dev.Id);
        ViewData["Categories"] = await _categoryService.GetAllAsync();
        ViewData["DeveloperId"] = dev.Id;

        return View(model);
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
        IFormFile? GameFile)
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
        IFormFile? GameFile, bool RemoveGameFile = false)
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

        var game = await _gameService.GetByIdAsync(GameId);
        if (game == null || game.DeveloperId != dev.Id)
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
            CoverImageUrl = CoverImageUrl,
            TrailerUrl = TrailerUrl,
            Developer = dev.Name,
            DeveloperId = dev.Id
        };

        await _gameService.UpdateAsync(GameId, update, CategoryIds ?? new());

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
}
