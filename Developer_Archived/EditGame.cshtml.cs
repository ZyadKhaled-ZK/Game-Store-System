using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using GameStore.DAL.DataBase;
using GameStore.DAL.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GameStore.PL.Pages.Developer
{
    public class EditGameModel : PageModel
    {
        private readonly GameStoreDbContext _context;
        private readonly IWebHostEnvironment _env;

        public EditGameModel(GameStoreDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [BindProperty] public GameInputModel Input { get; set; } = new();
        public List<Category> AvailableCategories { get; set; } = new();
        public string GameId { get; set; } = string.Empty;
        public List<string> ExistingScreenshots { get; set; } = new();
        public string? ExistingCoverUrl { get; set; }
        public string? ExistingGameFileName { get; set; }
        public long ExistingGameFileSizeBytes { get; set; }
        [TempData] public string StatusMessage { get; set; } = string.Empty;
        [TempData] public string ErrorMessage { get; set; } = string.Empty;

        public class GameInputModel
        {
            [Required, MinLength(2), MaxLength(200)] public string Title { get; set; } = string.Empty;
            [Required, MinLength(10)] public string Description { get; set; } = string.Empty;
            [Required, Range(0.0, 10000.0)] public decimal Price { get; set; }
            [Url]
            [RegularExpression(@"^(https?\:\/\/)?(www\.youtube\.com|youtu\.?be)\/.+$", ErrorMessage = "Must be a valid YouTube URL")]
            public string? TrailerUrl { get; set; }
            [Required, DataType(DataType.Date)] public DateTime ReleaseDate { get; set; } = DateTime.Today;
            [Required, MaxLength(200)] public string? Developer { get; set; }
            [Required] public List<string> SelectedCategoryIds { get; set; } = new();
            public IFormFile? CoverImage { get; set; }
            public IFormFile? GameFile { get; set; }
            public List<IFormFile> Images { get; set; } = new();
            public List<string> ScreenshotsToRemove { get; set; } = new();
            public bool RemoveGameFile { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (HttpContext.Session.GetString("UserId") == null) return RedirectToPage("/Auth/Login");
            var game = await _context.Games.Include(g => g.GameCategories).FirstOrDefaultAsync(g => g.Id == id);
            if (game == null) return NotFound();

            GameId = game.Id;
            ExistingScreenshots = game.ScreenshotUrls;
            ExistingCoverUrl = game.CoverImageUrl;
            ExistingGameFileName = game.GameFileName;
            ExistingGameFileSizeBytes = game.GameFileSizeBytes;
            AvailableCategories = await _context.Categories.AsNoTracking().ToListAsync();
            Input = new GameInputModel
            {
                Title = game.Title,
                Description = game.Description ?? string.Empty,
                Price = game.Price,
                TrailerUrl = game.TrailerUrl,
                ReleaseDate = game.ReleaseDate,
                Developer = game.Developer,
                SelectedCategoryIds = game.GameCategories.Select(gc => gc.CategoryId).ToList()
            };
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string id)
        {
            if (HttpContext.Session.GetString("UserId") == null) return RedirectToPage("/Auth/Login");

            if (!ModelState.IsValid)
            {
                AvailableCategories = await _context.Categories.AsNoTracking().ToListAsync();
                GameId = id;
                var g = await _context.Games.FindAsync(id);
                ExistingScreenshots = g?.ScreenshotUrls ?? new();
                ExistingCoverUrl = g?.CoverImageUrl;
                ExistingGameFileName = g?.GameFileName;
                ExistingGameFileSizeBytes = g?.GameFileSizeBytes ?? 0;
                return Page();
            }

            var game = await _context.Games.Include(g => g.GameCategories).FirstOrDefaultAsync(g => g.Id == id);
            if (game == null) return NotFound();

            var baseFolder = Path.Combine(_env.WebRootPath, "uploads", "games", id);
            var imgFolder = Path.Combine(baseFolder, "images");
            var filesFolder = Path.Combine(baseFolder, "files");

            // Remove selected screenshots
            foreach (var url in Input.ScreenshotsToRemove ?? new())
            {
                game.ScreenshotUrls.Remove(url);
                var fp = Path.Combine(_env.WebRootPath, url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(fp)) System.IO.File.Delete(fp);
            }

            // New cover image
            if (Input.CoverImage != null && Input.CoverImage.Length > 0)
            {
                var ext = Path.GetExtension(Input.CoverImage.FileName).ToLower();
                if (new[] { ".jpg", ".jpeg", ".png", ".webp" }.Contains(ext))
                {
                    Directory.CreateDirectory(imgFolder);
                    if (!string.IsNullOrEmpty(game.CoverImageUrl))
                    {
                        var old = Path.Combine(_env.WebRootPath, game.CoverImageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                        if (System.IO.File.Exists(old)) System.IO.File.Delete(old);
                    }
                    var fn = "cover" + ext;
                    using var fs = new FileStream(Path.Combine(imgFolder, fn), FileMode.Create);
                    await Input.CoverImage.CopyToAsync(fs);
                    game.CoverImageUrl = $"/uploads/games/{id}/images/{fn}";
                }
            }

            // New screenshots
            if (Input.Images?.Count > 0)
            {
                Directory.CreateDirectory(imgFolder);
                foreach (var file in Input.Images)
                {
                    if (file.Length <= 0) continue;
                    var ext = Path.GetExtension(file.FileName).ToLower();
                    if (!new[] { ".jpg", ".jpeg", ".png", ".webp" }.Contains(ext)) continue;
                    var fn = Guid.NewGuid().ToString() + ext;
                    using var fs = new FileStream(Path.Combine(imgFolder, fn), FileMode.Create);
                    await file.CopyToAsync(fs);
                    game.ScreenshotUrls.Add($"/uploads/games/{id}/images/{fn}");
                }
            }

            // Game file
            if (Input.RemoveGameFile && !string.IsNullOrEmpty(game.GameFileUrl))
            {
                var fp = Path.Combine(_env.WebRootPath, game.GameFileUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(fp)) System.IO.File.Delete(fp);
                game.GameFileUrl = null; game.GameFileName = null; game.GameFileSizeBytes = 0;
            }
            else if (Input.GameFile != null && Input.GameFile.Length > 0)
            {
                if (!string.IsNullOrEmpty(game.GameFileUrl))
                {
                    var fp = Path.Combine(_env.WebRootPath, game.GameFileUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                    if (System.IO.File.Exists(fp)) System.IO.File.Delete(fp);
                }
                Directory.CreateDirectory(filesFolder);
                game.GameFileName = Input.GameFile.FileName;
                game.GameFileSizeBytes = Input.GameFile.Length;
                var safeFileName = Guid.NewGuid().ToString() + Path.GetExtension(Input.GameFile.FileName).ToLower();
                using var fs = new FileStream(Path.Combine(filesFolder, safeFileName), FileMode.Create);
                await Input.GameFile.CopyToAsync(fs);
                game.GameFileUrl = $"/uploads/games/{id}/files/{safeFileName}";
            }

            game.Title = Input.Title.Trim();
            game.Description = Input.Description.Trim();
            game.Price = Input.Price;
            game.TrailerUrl = !string.IsNullOrWhiteSpace(Input.TrailerUrl) ? FormatYouTubeUrl(Input.TrailerUrl) : null;
            game.ReleaseDate = Input.ReleaseDate;
            game.Developer = Input.Developer?.Trim();

            _context.GameCategories.RemoveRange(game.GameCategories);
            _context.GameCategories.AddRange(Input.SelectedCategoryIds.Select(cid =>
                new GameCategory { GameId = id, CategoryId = cid }));

            await _context.SaveChangesAsync();
            StatusMessage = $"'{game.Title}' updated successfully!";
            return RedirectToPage("/Developer/ManageGames");
        }

        private static string FormatYouTubeUrl(string url)
        {
            var m = Regex.Match(url,
                @"(?:youtube\.com\/(?:[^\/]+\/.+\/|(?:v|e(?:mbed)?)\/|.*[?&]v=)|youtu\.be\/)([^""&?\/\s]{11})",
                RegexOptions.IgnoreCase);
            return m.Success ? $"https://www.youtube.com/embed/{m.Groups[1].Value}" : url;
        }
    }
}
