using System.Diagnostics;
using System.Text.Json;
using GameStore.PL.Models;
using GameStore.PL.Models.Home;
using GameStore.DAL.Enum;

namespace GameStore.PL.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IWebHostEnvironment _env;
    private readonly ICartService _cartService;
    private readonly IReviewService _reviewService;
    private readonly IGameService _gameService;
    private readonly ICategoryService _categoryService;
    private readonly IWishlistService _wishlistService;
    private readonly ILibraryService _libraryService;

    public HomeController(ILogger<HomeController> logger, IWebHostEnvironment env,
        ICartService cartService, IReviewService reviewService, IGameService gameService,
        ICategoryService categoryService, IWishlistService wishlistService,
        ILibraryService libraryService)
    {
        _logger = logger;
        _env = env;
        _cartService = cartService;
        _reviewService = reviewService;
        _gameService = gameService;
        _categoryService = categoryService;
        _wishlistService = wishlistService;
        _libraryService = libraryService;
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
            .ToDictionary(g => g.Key, g => g.Select(r => new ReviewDto
            {
                Username = r.User?.Username ?? "Anon",
                Rating = r.Rating,
                Comment = r.Comment ?? "",
                CreatedAt = r.CreatedAt.ToString("yyyy-MM-dd")
            }).ToList());

        model.ReviewsJson = JsonSerializer.Serialize(
            model.ReviewsByGame.ToDictionary(kv => kv.Key, kv => kv.Value.Select(r => new
            {
                username = r.Username,
                rating = r.Rating,
                comment = r.Comment,
                createdAt = r.CreatedAt
            }).ToList())
        );

        model.HeroGamesJson = JsonSerializer.Serialize(
            model.HeroGames.Select(g => new
            {
                id = g.Id,
                title = g.Title,
                developer = g.Developer ?? "UNKNOWN",
                price = g.Price,
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
    public async Task<IActionResult> Download(string id)
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Auth");

        var game = await _gameService.GetByIdAsync(id);
        if (game == null || string.IsNullOrEmpty(game.GameFileUrl))
            return NotFound();

        var owns = await _libraryService.HasGame(userId, id);
        if (!owns)
            return Unauthorized();

        var filePath = Path.Combine(_env.WebRootPath, game.GameFileUrl.TrimStart('/'));
        if (!System.IO.File.Exists(filePath))
            return NotFound();

        return PhysicalFile(filePath, "application/octet-stream", game.GameFileName ?? "game.zip");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddToCart([FromBody] string gameId)
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId))
            return Json(new { success = false, message = "Please login first." });

        var (success, message) = await _cartService.AddToCartAsync(userId, gameId);
        return Json(new { success, message });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleWishlist([FromBody] string gameId)
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId))
            return Json(new { success = false, message = "Please login first." });

        var inWishlist = await _wishlistService.IsInWishlistAsync(userId, gameId);

        if (inWishlist)
        {
            var items = await _wishlistService.GetWishlistAsync(userId);
            var item = items.FirstOrDefault(w => w.GameId == gameId);
            if (item != null) await _wishlistService.RemoveFromWishlistAsync(item.Id, userId);
            return Json(new { success = true, inWishlist = false });
        }
        else
        {
            var (success, message) = await _wishlistService.AddToWishlistAsync(userId, gameId);
            return Json(new { success, inWishlist = success, message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddReview([FromBody] ReviewRequest request)
    {
        if (!ModelState.IsValid)
            return Json(new { success = false, message = "Invalid input." });

        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId))
            return Json(new { success = false, message = "Please login first." });

        var (success, error) = await _reviewService.CreateAsync(userId, request.GameId, request.Rating, request.Comment);
        return Json(new { success, message = error });
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
