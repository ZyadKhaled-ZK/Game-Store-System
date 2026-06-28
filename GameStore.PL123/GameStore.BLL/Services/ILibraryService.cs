namespace GameStore.BLL.Services
{
    public interface ILibraryService
    {
        Task<List<LibraryGame>> GetLibraryGamesAsync(string userId);
        Task<bool> HasGame(string userId, string gameId);
        Task AddGameToLibraryAsync(string userId, string gameId);
    }
}
