using GameStore.BLL.Models;

namespace GameStore.BLL.Services
{
    public class GameService : IGameService
    {
        private readonly IUnitOfWork _uow;

        public GameService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<List<Game>> GetAllWithCategoriesAsync()
        {
            var games = await _uow.Repository<Game>().Query()
                .Include(g => g.DeveloperNav)
                .Include(g => g.GameCategories)
                    .ThenInclude(gc => gc.Category)
                .Where(g => g.DeveloperId == null || g.DeveloperNav == null || g.DeveloperNav.IsActive)
                .OrderByDescending(g => g.ReleaseDate)
                .AsNoTracking()
                .ToListAsync();

            foreach (var game in games)
                game.Developer ??= game.DeveloperNav?.Name;

            return games;
        }

        public async Task<List<Game>> GetHeroGamesAsync(int count = 5)
        {
            var games = await _uow.Repository<Game>().Query()
                .Include(g => g.DeveloperNav)
                .Include(g => g.GameCategories)
                    .ThenInclude(gc => gc.Category)
                .Where(g => g.DeveloperId == null || g.DeveloperNav == null || g.DeveloperNav.IsActive)
                .OrderByDescending(g => g.ReleaseDate)
                .Take(count)
                .AsNoTracking()
                .ToListAsync();

            foreach (var game in games)
                game.Developer ??= game.DeveloperNav?.Name;

            return games;
        }

        public async Task<PagedResult<Game>> GetPagedAsync(int page = 1, int pageSize = 12, string? search = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 12;
            if (pageSize > 100) pageSize = 100;

            var query = _uow.Repository<Game>().Query()
                .Include(g => g.DeveloperNav)
                .Include(g => g.GameCategories).ThenInclude(gc => gc.Category)
                .Where(g => g.DeveloperId == null || g.DeveloperNav == null || g.DeveloperNav.IsActive);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var q = search.Trim().ToLower();
                query = query.Where(g => g.Title.ToLower().Contains(q) || (g.Developer != null && g.Developer.ToLower().Contains(q)));
            }

            query = query.OrderByDescending(g => g.ReleaseDate).AsNoTracking();

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            foreach (var game in items)
                game.Developer ??= game.DeveloperNav?.Name;

            return new PagedResult<Game>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        public async Task<Game?> GetByIdAsync(string id)
        {
            return await _uow.Repository<Game>().Query()
                .Include(g => g.DeveloperNav)
                .Include(g => g.GameCategories)
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.Id == id);
        }

        public async Task<Game> CreateAsync(Game game, List<string> categoryIds)
        {
            game.CreatedAt = DateTime.UtcNow;
            game.UpdatedAt = DateTime.UtcNow;
            await _uow.Repository<Game>().AddAsync(game);

            if (categoryIds != null)
            {
                foreach (var catId in categoryIds)
                {
                    await _uow.Repository<GameCategory>().AddAsync(new GameCategory { GameId = game.Id, CategoryId = catId });
                }
            }

            await _uow.SaveChangesAsync();
            return game;
        }

        public async Task<Game?> UpdateAsync(string id, Game update, List<string> categoryIds)
        {
            var game = await _uow.Repository<Game>().Query()
                .Include(g => g.GameCategories)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (game == null) return null;

            game.Title = update.Title;
            game.Description = update.Description;
            game.Price = update.Price;
            game.ReleaseDate = update.ReleaseDate;
            game.Developer = update.Developer;
            game.DeveloperId = update.DeveloperId;
            game.CoverImageUrl = update.CoverImageUrl;
            game.TrailerUrl = update.TrailerUrl;
            game.UpdatedAt = DateTime.UtcNow;

            _uow.Repository<GameCategory>().RemoveRange(game.GameCategories);
            if (categoryIds != null)
            {
                foreach (var catId in categoryIds)
                {
                    await _uow.Repository<GameCategory>().AddAsync(new GameCategory { GameId = game.Id, CategoryId = catId });
                }
            }

            await _uow.SaveChangesAsync();
            return game;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var game = await _uow.Repository<Game>().GetByIdAsync(id);
            if (game == null) return false;

            _uow.Repository<Game>().Delete(game);
            await _uow.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetTotalGamesAsync()
        {
            return await _uow.Repository<Game>().CountAsync();
        }

        public async Task<int> GetFreeGamesCountAsync()
        {
            return await _uow.Repository<Game>().CountAsync(g => g.Price == 0);
        }

        public async Task<List<GamesByCategory>> GetGamesByCategoryAsync()
        {
            return await _uow.Repository<Category>().Query()
                .Select(c => new GamesByCategory
                {
                    Category = c.Name,
                    Count = c.GameCategories.Count
                })
                .OrderByDescending(g => g.Count)
                .ToListAsync();
        }
    }
}
