namespace GameStore.PL.Models.Admin;

public class ManageDevelopersViewModel
{
    public List<Developer> Developers { get; set; } = new();
    public string Message { get; set; } = string.Empty;
    public bool IsError { get; set; }
}

public class DeveloperDetailsViewModel
{
    public Developer Developer { get; set; } = null!;
    public List<Game> Games { get; set; } = new();
    public int TotalDownloads { get; set; }
    public int TotalReviews { get; set; }
    public int TotalRevenue { get; set; }
    public double AvgRating { get; set; }
}
