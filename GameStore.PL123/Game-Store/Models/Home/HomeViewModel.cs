namespace GameStore.PL.Models.Home;

public class ReviewDto
{
    public string Username { get; set; } = "";
    public int Rating { get; set; }
    public string Comment { get; set; } = "";
    public string CreatedAt { get; set; } = "";
}

public class HomeViewModel
{
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
    public HashSet<string> OwnedGameIds { get; set; } = new();
}
