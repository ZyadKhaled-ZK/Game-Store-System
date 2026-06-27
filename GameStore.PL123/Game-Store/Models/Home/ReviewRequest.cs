namespace GameStore.PL.Models.Home;

public class ReviewRequest
{
    public string GameId { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Comment { get; set; }
}
