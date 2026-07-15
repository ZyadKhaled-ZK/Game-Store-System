using Microsoft.EntityFrameworkCore;

namespace GameStore.BLL.Services;

public class GameAccessService : IGameAccessService
{
    private readonly IUnitOfWork _uow;

    public GameAccessService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public bool IsPreRelease(Game game)
    {
        return game.ReleaseDate > DateTime.UtcNow;
    }

    public async Task<bool> CanAccessPreRelease(Game game, string userId, Role role)
    {
        if (role == Role.ADMIN) return true;
        if (role != Role.DEVELOPER) return false;

        var dev = await _uow.Repository<Developer>().Query()
            .Where(d => d.UserId == userId)
            .Select(d => d.Id)
            .FirstOrDefaultAsync();

        return dev != null && dev == game.DeveloperId;
    }

    public async Task<HashSet<string>> GetPreviewableGameIdsAsync(string userId, Role role)
    {
        if (role == Role.ADMIN)
        {
            var adminIds = await _uow.Repository<Game>().Query()
                .Where(g => g.ReleaseDate > DateTime.UtcNow)
                .Select(g => g.Id)
                .ToListAsync();
            return adminIds.ToHashSet();
        }

        var devIds = await _uow.Repository<Developer>().Query()
            .Where(d => d.UserId == userId)
            .Select(d => d.Id)
            .ToListAsync();

        if (devIds.Count == 0)
            return new HashSet<string>();

        var ids = await _uow.Repository<Game>().Query()
            .Where(g => g.ReleaseDate > DateTime.UtcNow && devIds.Contains(g.DeveloperId))
            .Select(g => g.Id)
            .ToListAsync();

        return ids.ToHashSet();
    }
}
