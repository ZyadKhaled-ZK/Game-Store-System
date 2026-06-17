using GameStore.DAL.DataBase;
using GameStore.DAL.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GameStore.PL.Pages.Developer
{
    public class DashboardModel : PageModel
    {
        private readonly GameStoreDbContext _context;
        public DashboardModel(GameStoreDbContext context) => _context = context;

        public string Username { get; set; } = "Developer";
        public int TotalGames { get; set; }
        public int TotalCategories { get; set; }
        public int FreeGames { get; set; }
        public int GamesWithFiles { get; set; }
        public List<Game> RecentGames { get; set; } = new();

        public IActionResult OnGet()
        {
            if (HttpContext.Session.GetString("UserId") == null)
                return RedirectToPage("/Auth/Login");

            Username = HttpContext.Session.GetString("Username") ?? "Developer";
            TotalGames = _context.Games.Count();
            TotalCategories = _context.Categories.Count();
            FreeGames = _context.Games.Count(g => g.Price == 0m);
            GamesWithFiles = _context.Games.Count(g => g.GameFileUrl != null);
            RecentGames = _context.Games
                .Include(g => g.GameCategories)
                .OrderByDescending(g => g.ReleaseDate)
                .Take(5)
                .ToList();
            return Page();
        }
    }
}
