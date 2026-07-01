using System.Diagnostics;
using System.Text.Json;
using GameStore.PL.Models;
using GameStore.PL.Models.Home;
using GameStore.DAL.Enum;

namespace GameStore.PL.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IGameService _gameService;
    private readonly ICategoryService _categoryService;
    private readonly ICartService _cartService;
    private readonly IWishlistService _wishlistService;
    private readonly ILibraryService _libraryService;
    private readonly ISaleService _saleService;
    private readonly IReviewService _reviewService;
    private readonly IMapper _mapper;

    public HomeController(ILogger<HomeController> logger,
        IGameService gameService, ICategoryService categoryService,
        ICartService cartService, IWishlistService wishlistService,
        ILibraryService libraryService, ISaleService saleService,
        IReviewService reviewService, IMapper mapper)
    {
        _logger = logger;
        _gameService = gameService;
        _categoryService = categoryService;
        _cartService = cartService;
        _wishlistService = wishlistService;
        _libraryService = libraryService;
        _saleService = saleService;
        _reviewService = reviewService;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? page)
    {
        var model = new HomeViewModel
        {
            CurrentPage = page ?? 1
        };

        var paged = await _gameService.GetPagedAsync(model.CurrentPage, 12);
        model.Games = paged.Items;
        model.TotalPages = paged.TotalPages;
        model.TotalGames = paged.TotalCount;

        var allGames = await _gameService.GetAllWithCategoriesAsync();
        model.HeroGames = allGames.Take(5).ToList();
        model.FeaturedGame = model.HeroGames.FirstOrDefault();
        model.Categories = await _categoryService.GetAllAsync();

        var allGameIds = model.Games.Concat(model.HeroGames).Select(g => g.Id).Distinct().ToList();
        var activeSales = await _saleService.GetActiveSalesByGameIdsAsync(allGameIds);
        ViewData["ActiveSales"] = activeSales.ToDictionary(s => s.GameId, s => s.NewPrice);

        var userId = HttpContext.Session.GetString("UserId");
        if (!string.IsNullOrEmpty(userId))
        {
            var cartItems = await _cartService.GetCartItemsAsync(userId);
            model.CartJson = JsonSerializer.Serialize(cartItems.Select(ci => ci.GameId).ToList());

            var wishlistItems = await _wishlistService.GetWishlistAsync(userId);
            model.WishlistJson = JsonSerializer.Serialize(wishlistItems.Select(w => w.GameId).ToList());

            var owned = await _libraryService.GetLibraryGamesAsync(userId);
            model.OwnedGameIds = owned.Select(lg => lg.GameId).ToHashSet();
        }

        var allReviews = await _reviewService.GetAllWithDetailsAsync();
        model.ReviewsByGame = allReviews
            .GroupBy(r => r.GameId)
            .ToDictionary(g => g.Key, g => _mapper.Map<List<ReviewDto>>(g.ToList()));

        model.ReviewsJson = JsonSerializer.Serialize(
            model.ReviewsByGame.ToDictionary(kv => kv.Key, kv => kv.Value.Select(r => new
            {
                username = r.Username,
                rating = r.Rating,
                comment = r.Comment,
                createdAt = r.CreatedAt
            }).ToList())
        );

        var salePrices = ViewData["ActiveSales"] as Dictionary<string, decimal> ?? new();
        model.HeroGamesJson = JsonSerializer.Serialize(
            model.HeroGames.Select(g => new
            {
                id = g.Id,
                title = g.Title,
                developer = g.Developer ?? "UNKNOWN",
                price = g.Price,
                salePrice = salePrices.TryGetValue(g.Id, out var sp) ? sp : (decimal?)null,
                description = g.Description,
                trailerUrl = g.TrailerUrl,
                screenshots = g.ScreenshotUrls,
                coverImageUrl = g.CoverImageUrl,
                releaseDate = g.ReleaseDate.ToString("yyyy-MM-dd"),
                categories = g.GameCategories.Select(gc => gc.Category.Name).ToList(),
                hasFile = g.GameFileUrl != null,
                fileName = g.GameFileName,
                fileSizeBytes = g.GameFileSizeBytes
            }).ToList()
        );

        return View(model);
    }

    [HttpGet]
    public IActionResult Privacy()
    {
        return View();
    }

    [HttpGet]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
