namespace GameStore.PL.Models.Library;

public class LibraryViewModel
{
    public List<LibraryGame> LibraryGames { get; set; } = new();
    public string? CurrentUserId { get; set; }
    public string? CurrentUserRole { get; set; }
    public HashSet<string> PreviewableGameIds { get; set; } = new();
    public decimal AvailableCredit { get; set; }
    public Dictionary<string, List<GameVersionModel>> GameVersions { get; set; } = new();
}
