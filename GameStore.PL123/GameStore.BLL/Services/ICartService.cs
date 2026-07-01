namespace GameStore.BLL.Services
{
    public interface ICartService
    {
        Task<List<CartItem>> GetCartItemsAsync(string userId);
        Task<(bool Success, string Error)> AddToCartAsync(string userId, string gameId);
        Task<bool> RemoveFromCartAsync(string cartItemId, string userId);
    }
}
