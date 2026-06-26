namespace GameStore.BLL.Services
{
    public interface ICartService
    {
        Task<List<CartItem>> GetCartItemsAsync(string userId);
        Task<int> GetCartCountAsync(string userId);
        Task<(bool Success, string Error)> AddToCartAsync(string userId, string gameId);
        Task<bool> RemoveFromCartAsync(string cartItemId);
        Task ClearCartAsync(string userId);
    }
}
