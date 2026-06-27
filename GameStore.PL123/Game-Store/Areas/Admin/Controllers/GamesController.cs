using System.ComponentModel.DataAnnotations;
using GameStore.PL.Models.Admin;

namespace GameStore.PL.Areas.Admin.Controllers;

[Area("Admin")]
[ServiceFilter(typeof(AdminOnlyFilter))]
public class GamesController : Controller
{
    private readonly IGameService _gameService;
    private readonly IGameFileService _gameFileService;
    private readonly ICategoryService _categoryService;
    private readonly IWebHostEnvironment _env;

    public GamesController(IGameService gameService, IGameFileService gameFileService,
        ICategoryService categoryService, IWebHostEnvironment env)
    {
        _gameService = gameService;
        _gameFileService = gameFileService;
        _categoryService = categoryService;
        _env = env;
    }

    private async Task<ManageGamesViewModel> LoadViewModel()
    {
        var model = new ManageGamesViewModel();
        if (TempData.TryGetValue("Message", out var msg)) model.Message = msg?.ToString() ?? "";
        if (TempData.TryGetValue("IsError", out var err)) model.IsError = err is bool b && b;

        model.Games = await _gameService.GetAllWithCategoriesAsync();
        model.Categories = await _categoryService.GetAllAsync();

        model.GameDataJson = System.Text.Json.JsonSerializer.Serialize(model.Games.Select(g => new
        {
            id = g.Id,
            title = g.Title,
            description = g.Description ?? "",
            price = g.Price,
            developer = g.Developer ?? "",
            trailerUrl = g.TrailerUrl ?? "",
            coverImageUrl = g.CoverImageUrl ?? "",
            releaseDate = g.ReleaseDate.ToString("yyyy-MM-dd"),
            categoryIds = g.GameCategories.Select(gc => gc.CategoryId).ToList(),
            hasFile = g.GameFileUrl != null,
            fileName = g.GameFileName,
            fileSizeBytes = g.GameFileSizeBytes,
            screenshots = g.ScreenshotUrls
        }));

        return model;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var model = await LoadViewModel();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Required, StringLength(200)] string Title,
        [StringLength(4000)] string? Description,
        [Range(0, 9999.99)] decimal Price,
        [Required] DateTime ReleaseDate,
        [StringLength(200)] string? Developer,
        [StringLength(500)] string? CoverImageUrl,
        [StringLength(500)] string? TrailerUrl,
        List<string>? CategoryIds,
        IFormFile? GameFile)
    {
        if (!ModelState.IsValid) return RedirectToAction("Index");

        var game = new Game
        {
            Title = Title,
            Description = Description,
            Price = Price,
            ReleaseDate = ReleaseDate,
            Developer = Developer,
            CoverImageUrl = CoverImageUrl,
            TrailerUrl = TrailerUrl,
        };

        await _gameService.CreateAsync(game, CategoryIds ?? new());

        if (GameFile != null)
        {
            var (fileUrl, fileName, fileSize) = await SaveGameFileAsync(game.Id, GameFile);
            await _gameFileService.UpdateGameFileAsync(game.Id, fileUrl, fileName, fileSize);
        }

        TempData["Message"] = $"Game '{Title}' created.";
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
        [StringLength(200)] string? Developer,
        [StringLength(500)] string? CoverImageUrl,
        [StringLength(500)] string? TrailerUrl,
        List<string>? CategoryIds,
        IFormFile? GameFile, bool RemoveGameFile = false)
    {
        if (!ModelState.IsValid) return RedirectToAction("Index");

        var update = new Game
        {
            Title = Title,
            Description = Description,
            Price = Price,
            ReleaseDate = ReleaseDate,
            Developer = Developer,
            CoverImageUrl = CoverImageUrl,
            TrailerUrl = TrailerUrl,
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
        await DeleteGameFolderAsync(id);
        await _gameService.DeleteAsync(id);
        TempData["Message"] = "Game deleted.";
        TempData["IsError"] = false;
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddScreenshot([Required] string GameId, [Required, StringLength(500)] string ScreenshotUrl)
    {
        if (!ModelState.IsValid) return RedirectToAction("Index");
        await _gameFileService.AddScreenshotAsync(GameId, ScreenshotUrl);
        TempData["Message"] = "Screenshot added.";
        TempData["IsError"] = false;
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveScreenshot([Required] string GameId, [Required, StringLength(500)] string ScreenshotUrl)
    {
        if (!ModelState.IsValid) return RedirectToAction("Index");
        await _gameFileService.RemoveScreenshotAsync(GameId, ScreenshotUrl);
        TempData["Message"] = "Screenshot removed.";
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
