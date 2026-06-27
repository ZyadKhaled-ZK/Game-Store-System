namespace GameStore.PL.Models.Admin;

public class ManageGamesViewModel
{
    public List<Game> Games { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
    public string GameDataJson { get; set; } = "[]";
    public string Message { get; set; } = string.Empty;
    public bool IsError { get; set; }
}
