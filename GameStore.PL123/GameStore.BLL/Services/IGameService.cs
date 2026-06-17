namespace GameStore.BLL.Services
{
    public interface IGameService
    {
        Task<List<Game>> GetAllWithCategoriesAsync();
        Task<Game?> GetByIdAsync(string id);
        Task<Game> CreateAsync(Game game, List<string> categoryIds);
        Task<Game?> UpdateAsync(string id, Game update, List<string> categoryIds);
        Task<bool> DeleteAsync(string id);
        Task<int> GetTotalGamesAsync();
    }
}
