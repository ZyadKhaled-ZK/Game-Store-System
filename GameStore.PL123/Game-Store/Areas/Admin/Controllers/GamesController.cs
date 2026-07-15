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
    private readonly IDeveloperService _devService;
    private readonly IWebHostEnvironment _env;
    private readonly ISystemRequirementService _systemReqService;
    private readonly IGameVersionService _gameVersionService;

    public GamesController(IGameService gameService, IGameFileService gameFileService,
        ICategoryService categoryService, IDeveloperService devService, IWebHostEnvironment env,
        ISystemRequirementService systemReqService,
        IGameVersionService gameVersionService)
    {
        _gameService = gameService;
        _gameFileService = gameFileService;
        _categoryService = categoryService;
        _devService = devService;
        _env = env;
        _systemReqService = systemReqService;
        _gameVersionService = gameVersionService;
    }

    private async Task<ManageGamesViewModel> LoadViewModel()
    {
        var model = new ManageGamesViewModel();
        if (TempData.TryGetValue("Message", out var msg)) model.Message = msg?.ToString() ?? "";
        if (TempData.TryGetValue("IsError", out var err)) model.IsError = err is bool b && b;

        model.Games = await _gameService.GetAllWithCategoriesAsync();
        model.Categories = await _categoryService.GetAllAsync();
        model.Developers = await _devService.GetAllAsync();

        model.GameDataJson = System.Text.Json.JsonSerializer.Serialize(model.Games.Select(g => new
        {
            id = g.Id,
            title = g.Title,
            description = g.Description ?? "",
            price = g.Price,
            developer = g.Developer ?? "",
            developerId = g.DeveloperId ?? "",
            trailerUrl = g.TrailerUrl ?? "",
            coverImageUrl = g.CoverImageUrl ?? "",
            releaseDate = g.ReleaseDate.ToString("yyyy-MM-dd"),
            categoryIds = g.GameCategories.Select(gc => gc.CategoryId).ToList(),
            hasFile = g.GameFileUrl != null,
            fileName = g.GameFileName,
            fileSizeBytes = g.GameFileSizeBytes
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
        string? DeveloperId,
        [StringLength(500)] string? CoverImageUrl,
        [StringLength(500)] string? TrailerUrl,
        List<string>? CategoryIds,
        IFormFile? GameFile,
        IFormFile? CoverImageFile)
    {
        if (!ModelState.IsValid) return RedirectToAction("Index");

        var game = new Game
        {
            Title = Title,
            Description = Description,
            Price = Price,
            ReleaseDate = ReleaseDate,
            Developer = Developer,
            DeveloperId = DeveloperId,
            CoverImageUrl = CoverImageUrl,
            TrailerUrl = TrailerUrl,
        };

        if (CoverImageFile != null)
            game.CoverImageUrl = await SaveCoverImageFileAsync(game.Id, CoverImageFile);

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
        string? DeveloperId,
        [StringLength(500)] string? CoverImageUrl,
        [StringLength(500)] string? TrailerUrl,
        List<string>? CategoryIds,
        IFormFile? GameFile, bool RemoveGameFile = false,
        IFormFile? CoverImageFile = null)
    {
        if (!ModelState.IsValid) return RedirectToAction("Index");

        var oldGame = await _gameService.GetByIdAsync(GameId);

        var update = new Game
        {
            Title = Title,
            Description = Description,
            Price = Price,
            ReleaseDate = ReleaseDate,
            Developer = Developer,
            DeveloperId = DeveloperId,
            CoverImageUrl = CoverImageFile != null ? await SaveCoverImageFileAsync(GameId, CoverImageFile) : CoverImageUrl,
            TrailerUrl = TrailerUrl,
        };

        await _gameService.UpdateAsync(GameId, update, CategoryIds ?? new());

        if (CoverImageFile != null && oldGame?.CoverImageUrl != null)
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
        await DeleteGameFolderAsync(id);
        await _gameService.DeleteAsync(id);
        TempData["Message"] = "Game deleted.";
        TempData["IsError"] = false;
        return RedirectToAction("Index");
    }

    public class SaveRequirementsRequest
    {
        public string GameId { get; set; } = string.Empty;
        public SystemRequirementsModel Model { get; set; } = new();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveRequirements([FromBody] SaveRequirementsRequest request)
    {
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

    [HttpGet]
    public async Task<IActionResult> GetVersions(string id)
    {
        if (string.IsNullOrEmpty(id))
            return Json(new List<GameVersionModel>());
        var versions = await _gameVersionService.GetAllAsync(id);
        return Json(versions);
    }

    [HttpPost]
    public async Task<IActionResult> UploadVersion(string gameId, string versionLabel, string? changelog, IFormFile file)
    {
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
        var (success, message) = await _gameVersionService.DeleteAsync(gameId, versionId);
        return Json(new { success, message });
    }

    [HttpPost]
    public async Task<IActionResult> SetCurrentVersion(string gameId, string versionId)
    {
        var (success, message) = await _gameVersionService.SetCurrentAsync(gameId, versionId);
        return Json(new { success, message });
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
