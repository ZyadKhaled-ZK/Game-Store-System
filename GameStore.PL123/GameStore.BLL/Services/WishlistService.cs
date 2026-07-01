namespace GameStore.BLL.Services
{
    public class WishlistService : IWishlistService
    {
        private readonly IUnitOfWork _uow;

        public WishlistService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<List<WishlistItem>> GetWishlistAsync(string userId)
        {
            return await _uow.Repository<WishlistItem>().Query()
                .Include(w => w.Game)
                .Where(w => w.UserId == userId)
                .OrderByDescending(w => w.AddedAt)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<bool> IsInWishlistAsync(string userId, string gameId)
        {
            return await _uow.Repository<WishlistItem>().AnyAsync(w => w.UserId == userId && w.GameId == gameId);
        }

        public async Task<(bool Success, string Error)> AddToWishlistAsync(string userId, string gameId)
        {
            var game = await _uow.Repository<Game>().GetByIdAsync(gameId);
            if (game == null) return (false, "Game not found.");

            if (await IsInWishlistAsync(userId, gameId))
                return (false, "Game already in wishlist.");

            await _uow.Repository<WishlistItem>().AddAsync(new WishlistItem
            {
                UserId = userId,
                GameId = gameId
            });

            await _uow.SaveChangesAsync();
            return (true, string.Empty);
        }

        public async Task<bool> RemoveFromWishlistAsync(string wishlistItemId, string userId)
        {
            var item = await _uow.Repository<WishlistItem>().Query()
                .FirstOrDefaultAsync(w => w.Id == wishlistItemId && w.UserId == userId);
            if (item == null) return false;

            _uow.Repository<WishlistItem>().Delete(item);
            await _uow.SaveChangesAsync();
            return true;
        }
    }
}
