namespace GameStore.BLL.Services
{
    public interface IWishlistService
    {
        Task<List<WishlistItem>> GetWishlistAsync(string userId);
        Task<bool> IsInWishlistAsync(string userId, string gameId);
        Task<(bool Success, string Error)> AddToWishlistAsync(string userId, string gameId);
        Task<bool> RemoveFromWishlistAsync(string wishlistItemId);
        Task<int> GetWishlistCountAsync(string userId);
    }
}
