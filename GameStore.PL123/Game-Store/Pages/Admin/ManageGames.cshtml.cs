namespace GameStore.PL.Pages.Admin
{
    public class ManageGamesModel : PageModel
    {
        private readonly IGameService _gameService;
        private readonly ICategoryService _categoryService;

        public ManageGamesModel(IGameService gameService, ICategoryService categoryService)
        {
            _gameService = gameService;
            _categoryService = categoryService;
        }

        public List<Game>     Games      { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
        public string GameDataJson { get; set; } = "[]";

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
                categoryIds = g.GameCategories.Select(gc => gc.CategoryId).ToList()
            }));
        }

        public async Task<IActionResult> OnGet()
        {
            await LoadData();
            return Page();
        }

        public async Task<IActionResult> OnPostAddAsync(
            string Title, string? Description, decimal Price,
            DateTime ReleaseDate, string? Developer,
            string? CoverImageUrl, string? TrailerUrl,
            List<string>? CategoryIds)
        {
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
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEditAsync(
            string GameId, string Title, string? Description, decimal Price,
            DateTime ReleaseDate, string? Developer,
            string? CoverImageUrl, string? TrailerUrl,
            List<string>? CategoryIds)
        {
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
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(string id)
        {
            await _gameService.DeleteAsync(id);
            return RedirectToPage();
        }
    }
}
