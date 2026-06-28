using Microsoft.EntityFrameworkCore;

namespace GameStore.BLL.Services
{
    public class CartService : ICartService
    {
        private readonly IUnitOfWork _uow;

        public CartService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<List<CartItem>> GetCartItemsAsync(string userId)
        {
            var cart = await _uow.Repository<Cart>().Query()
                .Include(c => c.CartItems).ThenInclude(ci => ci.Game)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            return cart?.CartItems.ToList() ?? new();
        }

        public async Task<int> GetCartCountAsync(string userId)
        {
            var cart = await _uow.Repository<Cart>().Query()
                .AsNoTracking()
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            return cart?.CartItems.Count ?? 0;
        }

        public async Task<(bool Success, string Error)> AddToCartAsync(string userId, string gameId)
        {
            var game = await _uow.Repository<Game>().GetByIdAsync(gameId);
            if (game == null) return (false, "Game not found.");

            var alreadyOwned = await _uow.Repository<LibraryGame>()
                .AnyAsync(lg => lg.GameId == gameId
                    && _uow.Repository<Library>().Query().Any(l => l.Id == lg.LibraryId && l.UserId == userId));
            if (alreadyOwned) return (false, "You already own this game.");

            var cart = await _uow.Repository<Cart>().Query()
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                cart = new Cart { UserId = userId, CartItems = new List<CartItem>() };
                await _uow.Repository<Cart>().AddAsync(cart);
            }

            if (cart.CartItems.Any(ci => ci.GameId == gameId))
                return (false, "Game already in cart.");

            cart.CartItems.Add(new CartItem { CartId = cart.Id, GameId = gameId });
            await _uow.SaveChangesAsync();
            return (true, string.Empty);
        }

        public async Task<bool> RemoveFromCartAsync(string cartItemId, string userId)
        {
            var item = await _uow.Repository<CartItem>().Query()
                .Include(ci => ci.Cart)
                .FirstOrDefaultAsync(ci => ci.Id == cartItemId);

            if (item == null || item.Cart?.UserId != userId) return false;

            _uow.Repository<CartItem>().Delete(item);
            await _uow.SaveChangesAsync();
            return true;
        }

        public async Task ClearCartAsync(string userId)
        {
            var cart = await _uow.Repository<Cart>().Query()
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart != null)
            {
                _uow.Repository<CartItem>().RemoveRange(cart.CartItems);
                await _uow.SaveChangesAsync();
            }
        }
    }
}
