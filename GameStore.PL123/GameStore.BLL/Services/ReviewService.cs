namespace GameStore.BLL.Services
{
    public class ReviewService : IReviewService
    {
        private readonly IUnitOfWork _uow;
        private readonly ILibraryService _libraryService;

        public ReviewService(IUnitOfWork uow, ILibraryService libraryService)
        {
            _uow = uow;
            _libraryService = libraryService;
        }

        public async Task<List<Review>> GetAllWithDetailsAsync()
        {
            return await _uow.Repository<Review>().Query()
                .Include(r => r.User)
                .Include(r => r.Game)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var review = await _uow.Repository<Review>().GetByIdAsync(id);
            if (review == null) return false;

            _uow.Repository<Review>().Delete(review);
            await _uow.SaveChangesAsync();
            return true;
        }

        public async Task<List<Review>> GetByGameAsync(string gameId)
        {
            return await _uow.Repository<Review>().Query()
                .Include(r => r.User)
                .Where(r => r.GameId == gameId)
                .OrderByDescending(r => r.CreatedAt)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<Review>> GetByUserAsync(string userId)
        {
            return await _uow.Repository<Review>().Query()
                .Include(r => r.Game)
                .Include(r => r.User)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<(bool Success, string Error)> CreateAsync(string userId, string gameId, int rating, string? comment)
        {
            if (rating < 1 || rating > 5)
                return (false, "Rating must be between 1 and 5.");

            if (!await _libraryService.HasGame(userId, gameId))
                return (false, "You can only review games you have purchased.");

            if (await _uow.Repository<Review>().AnyAsync(r => r.UserId == userId && r.GameId == gameId))
                return (false, "You already reviewed this game.");

            await _uow.Repository<Review>().AddAsync(new Review
            {
                UserId = userId,
                GameId = gameId,
                Rating = rating,
                Comment = comment
            });

            await _uow.SaveChangesAsync();
            return (true, string.Empty);
        }
    }
}
