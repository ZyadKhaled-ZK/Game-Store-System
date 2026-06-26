namespace GameStore.BLL.Services
{
    public interface IReviewService
    {
        Task<List<Review>> GetAllWithDetailsAsync();
        Task<bool> DeleteAsync(string id);
        Task<List<Review>> GetByGameAsync(string gameId);
        Task<List<Review>> GetByUserAsync(string userId);
        Task<(bool Success, string Error)> CreateAsync(string userId, string gameId, int rating, string? comment);
    }
}
