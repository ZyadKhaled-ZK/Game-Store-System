namespace GameStore.BLL.Services
{
    public class OrderService : IOrderService
    {
        private readonly IUnitOfWork _uow;

        public OrderService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<List<Order>> GetAllWithDetailsAsync()
        {
            return await _uow.Repository<Order>().Query()
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Game)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<(bool Success, string Message)> PlaceOrderAsync(string userId)
        {
            await _uow.BeginTransactionAsync();
            try
            {
                var cart = await _uow.Repository<Cart>().Query()
                    .Include(c => c.CartItems).ThenInclude(ci => ci.Game)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart == null || !cart.CartItems.Any())
                {
                    await _uow.RollbackAsync();
                    return (false, "Your cart is empty.");
                }

                var order = new Order
                {
                    UserId = userId,
                    TotalPrice = cart.CartItems.Sum(ci => ci.Game?.Price ?? 0),
                };

                await _uow.Repository<Order>().AddAsync(order);

                foreach (var cartItem in cart.CartItems)
                {
                    await _uow.Repository<OrderItem>().AddAsync(new OrderItem
                    {
                        OrderId = order.Id,
                        GameId = cartItem.GameId,
                        PriceAtPurchase = cartItem.Game?.Price ?? 0
                    });
                }

                var library = await _uow.Repository<Library>().Query()
                    .Include(l => l.LibraryGames)
                    .FirstOrDefaultAsync(l => l.UserId == userId);

                if (library == null)
                {
                    library = new Library { UserId = userId };
                    await _uow.Repository<Library>().AddAsync(library);
                }

                foreach (var cartItem in cart.CartItems)
                {
                    if (!library.LibraryGames.Any(lg => lg.GameId == cartItem.GameId))
                    {
                        await _uow.Repository<LibraryGame>().AddAsync(new LibraryGame
                        {
                            LibraryId = library.Id,
                            GameId = cartItem.GameId
                        });
                    }
                }

                _uow.Repository<CartItem>().RemoveRange(cart.CartItems);
                await _uow.SaveChangesAsync();
                await _uow.CommitAsync();

                return (true, "Order placed successfully!");
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }
        }

        public async Task<Order?> GetOrderByIdAsync(string orderId)
        {
            return await _uow.Repository<Order>().Query()
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Game)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == orderId);
        }

        public async Task<Order?> GetByStripeSessionIdAsync(string sessionId)
        {
            return await _uow.Repository<Order>().Query()
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Game)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.StripeSessionId == sessionId);
        }

        public async Task<List<Order>> GetOrdersByUserAsync(string userId)
        {
            return await _uow.Repository<Order>().Query()
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Game)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<(bool Success, string Message, Order? Order)> CompleteCheckoutAsync(
            string userId, string stripeSessionId, string stripePaymentIntentId)
        {
            await _uow.BeginTransactionAsync();
            try
            {
                var existing = await _uow.Repository<Order>()
                    .AnyAsync(o => o.StripeSessionId == stripeSessionId);
                if (existing)
                {
                    await _uow.RollbackAsync();
                    return (false, "Order already completed.", null);
                }

                var cart = await _uow.Repository<Cart>().Query()
                    .Include(c => c.CartItems).ThenInclude(ci => ci.Game)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart == null || !cart.CartItems.Any())
                {
                    await _uow.RollbackAsync();
                    return (false, "Your cart is empty.", null);
                }

                var order = new Order
                {
                    UserId = userId,
                    TotalPrice = cart.CartItems.Sum(ci => ci.Game?.Price ?? 0),
                    PaymentStatus = PaymentStatus.Completed,
                    StripeSessionId = stripeSessionId,
                    StripePaymentIntentId = stripePaymentIntentId
                };

                await _uow.Repository<Order>().AddAsync(order);

                foreach (var cartItem in cart.CartItems)
                {
                    await _uow.Repository<OrderItem>().AddAsync(new OrderItem
                    {
                        OrderId = order.Id,
                        GameId = cartItem.GameId,
                        PriceAtPurchase = cartItem.Game?.Price ?? 0
                    });
                }

                var library = await _uow.Repository<Library>().Query()
                    .Include(l => l.LibraryGames)
                    .FirstOrDefaultAsync(l => l.UserId == userId);

                if (library == null)
                {
                    library = new Library { UserId = userId };
                    await _uow.Repository<Library>().AddAsync(library);
                }

                foreach (var cartItem in cart.CartItems)
                {
                    if (!library.LibraryGames.Any(lg => lg.GameId == cartItem.GameId))
                    {
                        await _uow.Repository<LibraryGame>().AddAsync(new LibraryGame
                        {
                            LibraryId = library.Id,
                            GameId = cartItem.GameId
                        });
                    }
                }

                _uow.Repository<CartItem>().RemoveRange(cart.CartItems);
                await _uow.SaveChangesAsync();
                await _uow.CommitAsync();

                return (true, "Order placed successfully!", order);
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }
        }
        public async Task<List<Order>> GetRecentWithDetailsAsync(int count)
        {
            return await _uow.Repository<Order>().Query()
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Game)
                .OrderByDescending(o => o.CreatedAt)
                .Take(count)
                .ToListAsync();
        }
    }
}
