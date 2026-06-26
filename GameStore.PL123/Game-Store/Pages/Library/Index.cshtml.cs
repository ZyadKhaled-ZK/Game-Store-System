namespace GameStore.PL.Pages.Library
{
    public class IndexModel : PageModel
    {
        private readonly ILibraryService _libraryService;

        public IndexModel(ILibraryService libraryService)
        {
            _libraryService = libraryService;
        }

        public List<LibraryGame> LibraryGames { get; set; } = new();

        private string UserId => HttpContext.Session.GetString("UserId") ?? string.Empty;

        public async Task<IActionResult> OnGet()
        {
            if (!string.IsNullOrEmpty(UserId))
                LibraryGames = await _libraryService.GetLibraryGamesAsync(UserId);
            return Page();
        }
    }
}
