using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using GameStore.DAL.DataBase;
using GameStore.DAL.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GameStore.PL.Pages.Developer
{
    public class AddGameModel : PageModel
    {
        private readonly GameStoreDbContext _context;
        private readonly IWebHostEnvironment _env;

        public AddGameModel(GameStoreDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [BindProperty] public GameInputModel Input { get; set; } = new();
        public List<Category> AvailableCategories { get; set; } = new();
        [TempData] public string StatusMessage { get; set; } = string.Empty;
        [TempData] public string ErrorMessage { get; set; } = string.Empty;

        public class GameInputModel
        {
            [Required(ErrorMessage = "Title is required"), MinLength(2), MaxLength(200)]
            public string Title { get; set; } = string.Empty;

            [Required(ErrorMessage = "Description is required"), MinLength(10)]
            public string Description { get; set; } = string.Empty;

            [Required(ErrorMessage = "Price is required"), Range(0.0, 10000.0)]
            public decimal Price { get; set; }

            [Url]
            [RegularExpression(@"^(https?\:\/\/)?(www\.youtube\.com|youtu\.?be)\/.+$", ErrorMessage = "Must be a valid YouTube URL")]
            public string? TrailerUrl { get; set; }

            [Required(ErrorMessage = "Release Date is required"), DataType(DataType.Date)]
            public DateTime ReleaseDate { get; set; } = DateTime.Today;

            [Required(ErrorMessage = "Developer name is required"), MaxLength(200)]
            [Display(Name = "Developer / Studio Name")]
            public string? Developer { get; set; }

            [Required(ErrorMessage = "Please select at least one category")]
            public List<string> SelectedCategoryIds { get; set; } = new();

            [Display(Name = "Cover Image")]
            public IFormFile? CoverImage { get; set; }

            [Display(Name = "Game File (.exe, .zip, .rar…)")]
            public IFormFile? GameFile { get; set; }

            [Display(Name = "Screenshots")]
            public List<IFormFile> Images { get; set; } = new();
        }

        public async Task<IActionResult> OnGetAsync()
        {
            if (HttpContext.Session.GetString("UserId") == null)
                return RedirectToPage("/Auth/Login");
            AvailableCategories = await _context.Categories.AsNoTracking().ToListAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (HttpContext.Session.GetString("UserId") == null)
                return RedirectToPage("/Auth/Login");

            if (!ModelState.IsValid)
            {
                ErrorMessage = "Please fix the highlighted fields.";
                AvailableCategories = await _context.Categories.AsNoTracking().ToListAsync();
                return Page();
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var gameId = Guid.NewGuid().ToString();
                var screenshotUrls = new List<string>();
                string? coverImageUrl = null;
                string? gameFileUrl = null;
                string? gameFileName = null;
                long gameFileSizeBytes = 0;

                var baseFolder = Path.Combine(_env.WebRootPath, "uploads", "games", gameId);
                Directory.CreateDirectory(baseFolder);

                // Cover image
                if (Input.CoverImage != null && Input.CoverImage.Length > 0)
                {
                    var ext = Path.GetExtension(Input.CoverImage.FileName).ToLower();
                    if (new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" }.Contains(ext))
                    {
                        var fn = "cover" + ext;
                        var imgFolder = Path.Combine(baseFolder, "images");
                        Directory.CreateDirectory(imgFolder);
                        using var fs = new FileStream(Path.Combine(imgFolder, fn), FileMode.Create);
                        await Input.CoverImage.CopyToAsync(fs);
                        coverImageUrl = $"/uploads/games/{gameId}/images/{fn}";
                    }
                }

                // Screenshots
                if (Input.Images != null)
                {
                    var imgFolder = Path.Combine(baseFolder, "images");
                    Directory.CreateDirectory(imgFolder);
                    foreach (var file in Input.Images)
                    {
                        if (file.Length > 0)
                        {
                            var ext = Path.GetExtension(file.FileName).ToLower();
                            if (new[] { ".jpg", ".jpeg", ".png", ".webp" }.Contains(ext))
                            {
                                var fn = Guid.NewGuid().ToString() + ext;
                                using var fs = new FileStream(Path.Combine(imgFolder, fn), FileMode.Create);
                                await file.CopyToAsync(fs);
                                screenshotUrls.Add($"/uploads/games/{gameId}/images/{fn}");
                            }
                        }
                    }
                }

                // Game file
                if (Input.GameFile != null && Input.GameFile.Length > 0)
                {
                    var filesFolder = Path.Combine(baseFolder, "files");
                    Directory.CreateDirectory(filesFolder);
                    gameFileName = Input.GameFile.FileName;
                    gameFileSizeBytes = Input.GameFile.Length;
                    var safeFileName = Guid.NewGuid().ToString() + Path.GetExtension(Input.GameFile.FileName).ToLower();
                    using var fs = new FileStream(Path.Combine(filesFolder, safeFileName), FileMode.Create);
                    await Input.GameFile.CopyToAsync(fs);
                    gameFileUrl = $"/uploads/games/{gameId}/files/{safeFileName}";
                }

                string? embedUrl = null;
                if (!string.IsNullOrWhiteSpace(Input.TrailerUrl))
                    embedUrl = FormatYouTubeUrl(Input.TrailerUrl);

                var newGame = new Game
                {
                    Id = gameId,
                    Title = Input.Title.Trim(),
                    Description = Input.Description.Trim(),
                    Price = Input.Price,
                    TrailerUrl = embedUrl,
                    ReleaseDate = Input.ReleaseDate,
                    Developer = Input.Developer?.Trim(),
                    CoverImageUrl = coverImageUrl,
                    ScreenshotUrls = screenshotUrls,
                    GameFileUrl = gameFileUrl,
                    GameFileName = gameFileName,
                    GameFileSizeBytes = gameFileSizeBytes
                };

                _context.Games.Add(newGame);
                if (Input.SelectedCategoryIds?.Any() == true)
                    _context.GameCategories.AddRange(Input.SelectedCategoryIds.Select(cid =>
                        new GameCategory { GameId = gameId, CategoryId = cid }));

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                StatusMessage = $"'{Input.Title}' published successfully!";
                return RedirectToPage("/Developer/Dashboard");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                ErrorMessage = "An error occurred: " + ex.Message;
                AvailableCategories = await _context.Categories.AsNoTracking().ToListAsync();
                return Page();
            }
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
