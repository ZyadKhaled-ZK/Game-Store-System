namespace GameStore.PL.Models.Admin;

public class UserDetailsViewModel
{
    public User User { get; set; } = null!;
    public List<Order> Orders { get; set; } = new();
    public List<LibraryGame> LibraryGames { get; set; } = new();
    public List<Review> Reviews { get; set; } = new();
}
