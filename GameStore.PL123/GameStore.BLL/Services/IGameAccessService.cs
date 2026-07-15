namespace GameStore.BLL.Services;

public interface IGameAccessService
{
    bool IsPreRelease(Game game);
    Task<bool> CanAccessPreRelease(Game game, string userId, Role role);
    Task<HashSet<string>> GetPreviewableGameIdsAsync(string userId, Role role);
}
