namespace GameStore.BLL.Services
{
    public interface IReviewService
    {
        Task<List<Review>> GetAllWithDetailsAsync();
        Task<PagedResult<Review>> GetAllPagedAsync(int page = 1, int pageSize = 50);
        Task<List<Review>> GetByGameIdsAsync(List<string> gameIds);
        Task<bool> DeleteAsync(string id);
        Task<List<Review>> GetByGameAsync(string gameId);
        Task<PagedResult<Review>> GetByDeveloperAsync(string developerId, int page, int pageSize = 10);
        Task<List<Review>> GetByUserAsync(string userId);
        Task<(bool Success, string Error)> CreateAsync(string userId, string gameId, int rating, string? comment);
    }
}
