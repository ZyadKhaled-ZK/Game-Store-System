using GameStore.DAL.DataBase;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GameStore.PL.Pages.Developer
{
    public class DeleteGameModel : PageModel
    {
        private readonly GameStoreDbContext _context;
        private readonly IWebHostEnvironment _env;

        public DeleteGameModel(GameStoreDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public string GameId { get; set; } = string.Empty;
        public string GameTitle { get; set; } = string.Empty;
        public string? GameScreenshot { get; set; }
        public string? GameCoverUrl { get; set; }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (HttpContext.Session.GetString("UserId") == null) return RedirectToPage("/Auth/Login");
            var game = await _context.Games.FindAsync(id);
            if (game == null) return NotFound();
            GameId = game.Id;
            GameTitle = game.Title;
            GameScreenshot = game.ScreenshotUrls.FirstOrDefault();
            GameCoverUrl = game.CoverImageUrl;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string id)
        {
            if (HttpContext.Session.GetString("UserId") == null) return RedirectToPage("/Auth/Login");
            var game = await _context.Games.Include(g => g.GameCategories).FirstOrDefaultAsync(g => g.Id == id);
            if (game == null) return NotFound();

            // Delete all uploaded files for this game
            var folder = Path.Combine(_env.WebRootPath, "uploads", "games", id);
            if (Directory.Exists(folder)) Directory.Delete(folder, true);

            _context.Games.Remove(game);
            await _context.SaveChangesAsync();
            TempData["StatusMessage"] = $"'{game.Title}' deleted successfully.";
            return RedirectToPage("/Developer/ManageGames");
        }
    }
}
