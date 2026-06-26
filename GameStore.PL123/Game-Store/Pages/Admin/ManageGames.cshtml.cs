using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GameStore.PL.Pages.Admin
{
    public class ManageGamesModel : PageModel
    {
        private readonly IGameService _gameService;
        private readonly IGameFileService _gameFileService;
        private readonly ICategoryService _categoryService;
        private readonly IWebHostEnvironment _env;

        public ManageGamesModel(IGameService gameService, IGameFileService gameFileService,
            ICategoryService categoryService, IWebHostEnvironment env)
        {
            _gameService = gameService;
            _gameFileService = gameFileService;
            _categoryService = categoryService;
            _env = env;
        }

        public List<Game>     Games      { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
        public string GameDataJson { get; set; } = "[]";
        public string Message { get; set; } = string.Empty;
        public bool IsError { get; set; }

        private async Task LoadData()
        {
            Games = await _gameService.GetAllWithCategoriesAsync();
            Categories = await _categoryService.GetAllAsync();

            GameDataJson = System.Text.Json.JsonSerializer.Serialize(Games.Select(g => new
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
        }

        public async Task<IActionResult> OnGet()
        {
            if (TempData.TryGetValue("Message", out var msg)) Message = msg?.ToString() ?? "";
            if (TempData.TryGetValue("IsError", out var err)) IsError = err is bool b && b;
            await LoadData();
            return Page();
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAddAsync(
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
            if (!ModelState.IsValid) return RedirectToPage();

            var game = new Game
            {
                Title         = Title,
                Description   = Description,
                Price         = Price,
                ReleaseDate   = ReleaseDate,
                Developer     = Developer,
                CoverImageUrl = CoverImageUrl,
                TrailerUrl    = TrailerUrl,
            };

            await _gameService.CreateAsync(game, CategoryIds ?? new());

            if (GameFile != null)
            {
                var (fileUrl, fileName, fileSize) = await SaveGameFileAsync(game.Id, GameFile);
                await _gameFileService.UpdateGameFileAsync(game.Id, fileUrl, fileName, fileSize);
            }

            TempData["Message"] = $"Game '{Title}' created.";
            TempData["IsError"] = false;
            return RedirectToPage();
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostEditAsync(
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
            if (!ModelState.IsValid) return RedirectToPage();

            var update = new Game
            {
                Title         = Title,
                Description   = Description,
                Price         = Price,
                ReleaseDate   = ReleaseDate,
                Developer     = Developer,
                CoverImageUrl = CoverImageUrl,
                TrailerUrl    = TrailerUrl,
            };

            await _gameService.UpdateAsync(GameId, update, CategoryIds ?? new());

            // Remove existing file if requested
            if (RemoveGameFile)
            {
                await DeleteGameFileAsync(GameId);
                await _gameFileService.ClearGameFileAsync(GameId);
            }

            // Upload new file if provided
            if (GameFile != null)
            {
                await DeleteGameFileAsync(GameId);
                var (fileUrl, fileName, fileSize) = await SaveGameFileAsync(GameId, GameFile);
                await _gameFileService.UpdateGameFileAsync(GameId, fileUrl, fileName, fileSize);
            }

            TempData["Message"] = $"Game '{Title}' updated.";
            TempData["IsError"] = false;
            return RedirectToPage();
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostDeleteAsync([Required] string id)
        {
            if (!ModelState.IsValid) return RedirectToPage();
            await DeleteGameFolderAsync(id);
            await _gameService.DeleteAsync(id);
            TempData["Message"] = "Game deleted.";
            TempData["IsError"] = false;
            return RedirectToPage();
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAddScreenshotAsync([Required] string GameId, [Required, StringLength(500)] string ScreenshotUrl)
        {
            if (!ModelState.IsValid) return RedirectToPage();
            await _gameFileService.AddScreenshotAsync(GameId, ScreenshotUrl);
            TempData["Message"] = "Screenshot added.";
            TempData["IsError"] = false;
            return RedirectToPage();
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostRemoveScreenshotAsync([Required] string GameId, [Required, StringLength(500)] string ScreenshotUrl)
        {
            if (!ModelState.IsValid) return RedirectToPage();
            await _gameFileService.RemoveScreenshotAsync(GameId, ScreenshotUrl);
            TempData["Message"] = "Screenshot removed.";
            TempData["IsError"] = false;
            return RedirectToPage();
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
            {
                Directory.Delete(folder, recursive: true);
            }
            return Task.CompletedTask;
        }

        private Task DeleteGameFolderAsync(string gameId)
        {
            var folder = Path.Combine(_env.WebRootPath, "uploads", "games", gameId);
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, recursive: true);
            }
            return Task.CompletedTask;
        }
    }
}
