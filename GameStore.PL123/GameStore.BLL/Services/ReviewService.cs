namespace GameStore.BLL.Services
{
    public class ReviewService : IReviewService
    {
        private readonly GameStoreDbContext _context;

        public ReviewService(GameStoreDbContext context)
        {
            _context = context;
        }

        public async Task<List<Review>> GetAllWithDetailsAsync()
        {
            return await _context.Reviews
                .Include(r => r.User)
                .Include(r => r.Game)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null) return false;

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
