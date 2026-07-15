using Microsoft.EntityFrameworkCore;

namespace GameStore.BLL.Services
{
    public class LibraryService : ILibraryService
    {
        private readonly IUnitOfWork _uow;

        public LibraryService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<List<LibraryGame>> GetLibraryGamesAsync(string userId)
        {
            var library = await _uow.Repository<Library>().Query()
                .Include(l => l.LibraryGames).ThenInclude(lg => lg.Game).ThenInclude(g => g.DeveloperNav)
                .Include(l => l.LibraryGames).ThenInclude(lg => lg.Game).ThenInclude(g => g.GameCategories).ThenInclude(gc => gc.Category)
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.UserId == userId);

            var games = library?.LibraryGames.OrderByDescending(lg => lg.AddedAt).ToList() ?? new();
            foreach (var lg in games)
                lg.Game.Developer ??= lg.Game.DeveloperNav?.Name;
            return games;
        }

        public async Task<bool> HasGame(string userId, string gameId)
        {
            return await _uow.Repository<LibraryGame>().AnyAsync(lg => lg.Library!.UserId == userId && lg.GameId == gameId);
        }

        public async Task AddGameToLibraryAsync(string userId, string gameId)
        {
            if (await HasGame(userId, gameId)) return;

            var libRepo = _uow.Repository<Library>();
            var lib = await libRepo.Query().FirstOrDefaultAsync(l => l.UserId == userId);
            if (lib == null)
            {
                lib = new Library { UserId = userId };
                await libRepo.AddAsync(lib);
            }

            await _uow.Repository<LibraryGame>().AddAsync(new LibraryGame
            {
                LibraryId = lib.Id,
                GameId = gameId,
                AddedAt = DateTime.UtcNow
            });
            await _uow.SaveChangesAsync();
        }

        public async Task RemoveGameFromLibraryAsync(string userId, string gameId)
        {
            var lg = await _uow.Repository<LibraryGame>().Query()
                .FirstOrDefaultAsync(x => x.Library!.UserId == userId && x.GameId == gameId);
            if (lg != null)
            {
                _uow.Repository<LibraryGame>().Delete(lg);
                await _uow.SaveChangesAsync();
            }
        }
    }
}
