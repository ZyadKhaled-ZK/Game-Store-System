namespace GameStore.BLL.Services
{
    public interface IDeveloperService
    {
        Task<Developer?> GetByUserIdAsync(string userId);
        Task<Developer?> GetByIdAsync(string id);
        Task<Developer?> GetBySlugAsync(string slug);
        Task<List<Developer>> GetAllAsync();
        Task<List<Game>> GetGamesAsync(string developerId);
        Task<(bool Success, string Error)> CreateOrUpdateProfileAsync(string userId, string name, string? slug, string? description, string? website, string? logoUrl, string? country);
    Task<(int GameCount, int TotalDownloads, int TotalReviews, int TotalRevenue, double AvgRating)> GetDashboardStatsAsync(string developerId);
    Task<List<(Game Game, int Downloads, double AvgRating, int ReviewCount)>> GetGameStatsAsync(string developerId);
    Task<bool> IsDeveloperUserAsync(string userId);
    Task<(bool Success, string Error)> DeleteAsync(string developerId);
    Task<(bool Success, string Error)> DemoteAsync(string developerId, string? currentUserId = null);
    Task<(bool Success, string Error)> ReactivateAsync(string developerId, string? currentUserId = null);
    }
}
