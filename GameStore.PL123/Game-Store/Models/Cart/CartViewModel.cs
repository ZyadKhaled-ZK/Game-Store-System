namespace GameStore.PL.Models.Cart;

public class CartViewModel
{
    public List<CartItem> CartItems { get; set; } = new();
    public decimal TotalPrice => CartItems.Sum(ci => ci.Game?.Price ?? 0);
    public string Message { get; set; } = string.Empty;
    public bool IsError { get; set; }
}
