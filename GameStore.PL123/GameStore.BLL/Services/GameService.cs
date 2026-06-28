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
            return await _uow.Repository<Game>().Query()
                .Include(g => g.GameCategories)
                    .ThenInclude(gc => gc.Category)
                .Where(g => g.DeveloperId == null || g.DeveloperNav == null || g.DeveloperNav.IsActive)
                .OrderByDescending(g => g.ReleaseDate)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<PagedResult<Game>> GetPagedAsync(int page = 1, int pageSize = 12)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 12;
            if (pageSize > 100) pageSize = 100;

            var query = _uow.Repository<Game>().Query()
                .Include(g => g.GameCategories).ThenInclude(gc => gc.Category)
                .Where(g => g.DeveloperId == null || g.DeveloperNav == null || g.DeveloperNav.IsActive)
                .OrderByDescending(g => g.ReleaseDate)
                .AsNoTracking();

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

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
                .Include(g => g.GameCategories)
                .FirstOrDefaultAsync(g => g.Id == id);
        }

        public async Task<Game> CreateAsync(Game game, List<string> categoryIds)
        {
            await _uow.Repository<Game>().AddAsync(game);
            await _uow.SaveChangesAsync();

            if (categoryIds != null)
            {
                foreach (var catId in categoryIds)
                {
                    await _uow.Repository<GameCategory>().AddAsync(new GameCategory { GameId = game.Id, CategoryId = catId });
                }
                await _uow.SaveChangesAsync();
            }

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
