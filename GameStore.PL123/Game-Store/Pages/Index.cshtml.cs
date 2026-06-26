using System.ComponentModel.DataAnnotations;

namespace GameStore.PL.Pages
{
    public class IndexModel : PageModel
    {
        public class ReviewDto
        {
            public string Username { get; set; } = "";
            public int Rating { get; set; }
            public string Comment { get; set; } = "";
            public string CreatedAt { get; set; } = "";
        }

        private readonly IWebHostEnvironment _env;
        private readonly ICartService _cartService;
        private readonly IReviewService _reviewService;
        private readonly IGameService _gameService;
        private readonly ICategoryService _categoryService;
        private readonly IWishlistService _wishlistService;

        public IndexModel(IWebHostEnvironment env, ICartService cartService, IReviewService reviewService,
            IGameService gameService, ICategoryService categoryService, IWishlistService wishlistService)
        {
            _env = env;
            _cartService = cartService;
            _reviewService = reviewService;
            _gameService = gameService;
            _categoryService = categoryService;
            _wishlistService = wishlistService;
        }

        public List<Game> Games { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
        public int TotalGames { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public Game? FeaturedGame { get; set; }
        public List<Game> HeroGames { get; set; } = new();
        public string HeroGamesJson { get; set; } = "[]";
        public string CartJson { get; set; } = "[]";
        public string ReviewsJson { get; set; } = "{}";
        public string WishlistJson { get; set; } = "[]";
        public Dictionary<string, List<ReviewDto>> ReviewsByGame { get; set; } = new();

        public async Task OnGet(int? page)
        {
            CurrentPage = page ?? 1;
            var paged = await _gameService.GetPagedAsync(CurrentPage, 12);
            Games = paged.Items;
            TotalPages = paged.TotalPages;
            TotalGames = paged.TotalCount;

            var allGames = await _gameService.GetAllWithCategoriesAsync();
            HeroGames = allGames.Take(5).ToList();
            FeaturedGame = HeroGames.FirstOrDefault();
            Categories = await _categoryService.GetAllAsync();

            var userId = HttpContext.Session.GetString("UserId");
            if (!string.IsNullOrEmpty(userId))
            {
                var cartItems = await _cartService.GetCartItemsAsync(userId);
                CartJson = System.Text.Json.JsonSerializer.Serialize(cartItems.Select(ci => ci.GameId).ToList());

                var wishlistItems = await _wishlistService.GetWishlistAsync(userId);
                WishlistJson = System.Text.Json.JsonSerializer.Serialize(wishlistItems.Select(w => w.GameId).ToList());
            }

            var allReviews = await _reviewService.GetAllWithDetailsAsync();

            ReviewsByGame = allReviews
                .GroupBy(r => r.GameId)
                .ToDictionary(g => g.Key, g => g.Select(r => new ReviewDto
                {
                    Username = r.User?.Username ?? "Anon",
                    Rating = r.Rating,
                    Comment = r.Comment ?? "",
                    CreatedAt = r.CreatedAt.ToString("yyyy-MM-dd")
                }).ToList());

            ReviewsJson = System.Text.Json.JsonSerializer.Serialize(
                ReviewsByGame.ToDictionary(kv => kv.Key, kv => kv.Value.Select(r => new
                {
                    username = r.Username,
                    rating = r.Rating,
                    comment = r.Comment,
                    createdAt = r.CreatedAt
                }).ToList())
            );

            HeroGamesJson = System.Text.Json.JsonSerializer.Serialize(
                HeroGames.Select(g => new
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
        }

        public async Task<IActionResult> OnGetDownloadAsync(string id)
        {
            var game = await _gameService.GetByIdAsync(id);
            if (game == null || string.IsNullOrEmpty(game.GameFileUrl))
                return NotFound();

            var filePath = Path.Combine(_env.WebRootPath, game.GameFileUrl.TrimStart('/'));
            if (!System.IO.File.Exists(filePath))
                return NotFound();

            return PhysicalFile(filePath, "application/octet-stream", game.GameFileName ?? "game.zip");
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAddToCartAsync([Required] string gameId)
        {
            if (!ModelState.IsValid)
                return new JsonResult(new { success = false, message = "Invalid request." });

            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
                return new JsonResult(new { success = false, message = "Please login first." });

            var (success, message) = await _cartService.AddToCartAsync(userId, gameId);
            return new JsonResult(new { success, message });
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostToggleWishlistAsync([Required] string gameId)
        {
            if (!ModelState.IsValid)
                return new JsonResult(new { success = false, message = "Invalid request." });

            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
                return new JsonResult(new { success = false, message = "Please login first." });

            var inWishlist = await _wishlistService.IsInWishlistAsync(userId, gameId);

            if (inWishlist)
            {
                var items = await _wishlistService.GetWishlistAsync(userId);
                var item = items.FirstOrDefault(w => w.GameId == gameId);
                if (item != null) await _wishlistService.RemoveFromWishlistAsync(item.Id);
                return new JsonResult(new { success = true, inWishlist = false });
            }
            else
            {
                var (success, message) = await _wishlistService.AddToWishlistAsync(userId, gameId);
                return new JsonResult(new { success, inWishlist = success, message });
            }
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAddReviewAsync([Required] string gameId, [Range(1, 5)] int rating, [StringLength(2000)] string? comment)
        {
            if (!ModelState.IsValid)
                return new JsonResult(new { success = false, message = "Invalid input." });

            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
                return new JsonResult(new { success = false, message = "Please login first." });

            var (success, error) = await _reviewService.CreateAsync(userId, gameId, rating, comment);
            return new JsonResult(new { success, message = error });
        }
    }
}
