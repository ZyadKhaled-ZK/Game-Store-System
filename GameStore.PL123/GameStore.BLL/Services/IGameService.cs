using GameStore.BLL.Models;

namespace GameStore.BLL.Services
{
    public interface IGameService
    {
        Task<List<Game>> GetAllWithCategoriesAsync();
        Task<PagedResult<Game>> GetPagedAsync(int page = 1, int pageSize = 12);
        Task<Game?> GetByIdAsync(string id);
        Task<Game> CreateAsync(Game game, List<string> categoryIds);
        Task<Game?> UpdateAsync(string id, Game update, List<string> categoryIds);
        Task<bool> DeleteAsync(string id);
        Task<int> GetTotalGamesAsync();
        Task<int> GetFreeGamesCountAsync();
        Task<List<GamesByCategory>> GetGamesByCategoryAsync();
    }
}
