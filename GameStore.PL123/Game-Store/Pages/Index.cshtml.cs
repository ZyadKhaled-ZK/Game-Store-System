namespace GameStore.PL.Pages
{
    public class IndexModel : PageModel
    {
        private readonly GameStoreDbContext _context;
        public IndexModel(GameStoreDbContext context) => _context = context;

        public List<Game> Games { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
        public int TotalGames { get; set; }
        public Game? FeaturedGame { get; set; }

        public void OnGet()
        {
            Games = _context.Games
                .Include(g => g.GameCategories).ThenInclude(gc => gc.Category)
                .OrderByDescending(g => g.ReleaseDate)
                .ToList();

            FeaturedGame = Games.FirstOrDefault();
            Categories = _context.Categories.OrderBy(c => c.Name).ToList();
            TotalGames = Games.Count;
        }
    }
}
