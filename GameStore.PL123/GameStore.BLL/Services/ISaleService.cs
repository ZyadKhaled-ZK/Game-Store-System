namespace GameStore.BLL.Services
{
    public interface ISaleService
    {
        Task<Sale?> GetByIdAsync(string id);
        Task<List<Sale>> GetPendingAsync();
        Task<List<Sale>> GetByDeveloperAsync(string developerId);
        Task<PagedResult<Sale>> GetByDeveloperAsync(string developerId, int page, int pageSize = 10);
        Task<List<Sale>> GetActiveSalesByGameIdsAsync(List<string> gameIds);
        Task<(bool Success, string Error)> CreateAsync(string developerId, string gameId, decimal newPrice, DateTime startDate, DateTime endDate);
        Task<(bool Success, string Error)> ApproveAsync(string id);
        Task<(bool Success, string Error)> RejectAsync(string id, string reason);
    }
}
