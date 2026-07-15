using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Caching.Distributed;
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
    private readonly IGameAccessService _gameAccess;
    private readonly ISystemRequirementService _systemReqService;
    private readonly IGameVersionService _gameVersionService;
    private readonly IDistributedCache _cache;

    public HomeController(ILogger<HomeController> logger,
        IGameService gameService, ICategoryService categoryService,
        ICartService cartService, IWishlistService wishlistService,
        ILibraryService libraryService, ISaleService saleService,
        IReviewService reviewService, IMapper mapper,
        IGameAccessService gameAccess,
        ISystemRequirementService systemReqService,
        IGameVersionService gameVersionService,
        IDistributedCache cache)
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
        _gameAccess = gameAccess;
        _systemReqService = systemReqService;
        _gameVersionService = gameVersionService;
        _cache = cache;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? page, string? search)
    {
        var model = await BuildHomeModelAsync(page ?? 1, search);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> GetGameGrid(int page = 1, string? search = null)
    {
        var model = await BuildGridModelAsync(page, search);
        return PartialView("_GameGridPartial", model);
    }

    private async Task<HomeViewModel> BuildHomeModelAsync(int page, string? search = null)
    {
        var model = new HomeViewModel { CurrentPage = page, SearchQuery = search };
        var paged = await _gameService.GetPagedAsync(page, 4, search);
        model.Games = paged.Items;
        model.TotalPages = paged.TotalPages;
        model.TotalGames = paged.TotalCount;

        model.HeroGames = await _gameService.GetHeroGamesAsync(5);
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

            var roleStr = HttpContext.Session.GetString("Role") ?? "CUSTOMER";
            var role = Enum.Parse<Role>(roleStr);
            model.PreviewableGameIds = await _gameAccess.GetPreviewableGameIdsAsync(userId, role);
        }

        model.PreReleaseGameIds = model.Games
            .Concat(model.HeroGames)
            .Where(g => _gameAccess.IsPreRelease(g))
            .Select(g => g.Id)
            .ToHashSet();

        var reviews = await _reviewService.GetByGameIdsAsync(allGameIds);
        model.ReviewsByGame = reviews
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
                coverImageUrl = g.CoverImageUrl,
                releaseDate = g.ReleaseDate.ToString("yyyy-MM-dd"),
                categories = g.GameCategories.Select(gc => gc.Category.Name).ToList(),
                hasFile = g.GameFileUrl != null,
                fileName = g.GameFileName,
                fileSizeBytes = g.GameFileSizeBytes,
                isPreRelease = model.PreReleaseGameIds.Contains(g.Id)
            }).ToList()
        );

        return model;
    }

    private async Task<HomeViewModel> BuildGridModelAsync(int page, string? search = null)
    {
        var model = new HomeViewModel { CurrentPage = page, SearchQuery = search };

        var paged = await _gameService.GetPagedAsync(page, 4, search);
        model.Games = paged.Items;
        model.TotalPages = paged.TotalPages;
        model.TotalGames = paged.TotalCount;

        var gameIds = model.Games.Select(g => g.Id).ToList();
        var activeSales = await _saleService.GetActiveSalesByGameIdsAsync(gameIds);
        ViewData["ActiveSales"] = activeSales.ToDictionary(s => s.GameId, s => s.NewPrice);

        var userId = HttpContext.Session.GetString("UserId");
        if (!string.IsNullOrEmpty(userId))
        {
            var owned = await _libraryService.GetLibraryGamesAsync(userId);
            model.OwnedGameIds = owned.Select(lg => lg.GameId).ToHashSet();

            var roleStr = HttpContext.Session.GetString("Role") ?? "CUSTOMER";
            var role = Enum.Parse<Role>(roleStr);
            model.PreviewableGameIds = await _gameAccess.GetPreviewableGameIdsAsync(userId, role);
        }

        model.PreReleaseGameIds = model.Games
            .Where(g => _gameAccess.IsPreRelease(g))
            .Select(g => g.Id)
            .ToHashSet();

        return model;
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

    [HttpGet]
    public async Task<IActionResult> GetRequirements(string id)
    {
        var reqs = await _systemReqService.GetAsync(id);
        return Json(reqs ?? new SystemRequirementsModel());
    }

    [HttpGet]
    public async Task<IActionResult> GetVersions(string id)
    {
        var versions = await _gameVersionService.GetAllAsync(id);
        return Json(versions);
    }

    [HttpGet]
    public async Task<IActionResult> TestRedis()
    {
        var cacheType = _cache.GetType().Name;
        try
        {
            await _cache.SetStringAsync("redis_test", "OK", new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30)
            });
            var result = await _cache.GetStringAsync("redis_test");

            if (result == "OK")
                return Content($"Redis: Connected ✅ (using {cacheType})");
            else
                return Content($"Redis: Write failed ❌ (using {cacheType})");
        }
        catch (Exception ex)
        {
            return Content($"Redis: Error ❌ — {ex.GetType().Name}: {ex.Message}");
        }
    }
}
