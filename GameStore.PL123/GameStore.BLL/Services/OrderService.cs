namespace GameStore.BLL.Services
{
    public class OrderService : IOrderService
    {
        private readonly GameStoreDbContext _context;

        public OrderService(GameStoreDbContext context)
        {
            _context = context;
        }

        public async Task<List<Order>> GetAllWithDetailsAsync()
        {
            return await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Game)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> UpdateStatusAsync(string orderId, OrderStatus status)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return false;

            order.Status = status;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetTotalOrdersAsync()
        {
            return await _context.Orders.CountAsync();
        }

        public async Task<int> GetCompletedOrdersAsync()
        {
            return await _context.Orders.CountAsync(o => o.Status == OrderStatus.COMPLETED);
        }

        public async Task<decimal> GetTotalRevenueAsync()
        {
            return await _context.Orders
                .Where(o => o.Status == OrderStatus.COMPLETED)
                .SumAsync(o => o.TotalPrice);
        }
    }
}
