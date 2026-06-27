namespace GameStore.PL.Models.Wishlist;

public class WishlistViewModel
{
    public List<WishlistItem> WishlistItems { get; set; } = new();
    public string Message { get; set; } = string.Empty;
    public bool IsError { get; set; }
}
