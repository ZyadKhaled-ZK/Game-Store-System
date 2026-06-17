using GameStore.DAL.DataBase;
using GameStore.DAL.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GameStore.PL.Pages.Developer
{
    public class ManageGamesModel : PageModel
    {
        private readonly GameStoreDbContext _context;
        public ManageGamesModel(GameStoreDbContext context) => _context = context;

        public List<Game> Games { get; set; } = new();

        public IActionResult OnGet()
        {
            if (HttpContext.Session.GetString("UserId") == null)
                return RedirectToPage("/Auth/Login");

            Games = _context.Games
                .Include(g => g.GameCategories).ThenInclude(gc => gc.Category)
                .OrderByDescending(g => g.ReleaseDate)
                .ToList();
            return Page();
        }
    }
}
