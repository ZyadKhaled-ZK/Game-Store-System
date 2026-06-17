namespace GameStore.BLL.Services
{
    public class GameService : IGameService
    {
        private readonly GameStoreDbContext _context;

        public GameService(GameStoreDbContext context)
        {
            _context = context;
        }

        public async Task<List<Game>> GetAllWithCategoriesAsync()
        {
            return await _context.Games
                .Include(g => g.GameCategories)
                    .ThenInclude(gc => gc.Category)
                .OrderByDescending(g => g.ReleaseDate)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Game?> GetByIdAsync(string id)
        {
            return await _context.Games
                .Include(g => g.GameCategories)
                .FirstOrDefaultAsync(g => g.Id == id);
        }

        public async Task<Game> CreateAsync(Game game, List<string> categoryIds)
        {
            _context.Games.Add(game);
            await _context.SaveChangesAsync();

            if (categoryIds != null)
            {
                foreach (var catId in categoryIds)
                {
                    _context.GameCategories.Add(new GameCategory { GameId = game.Id, CategoryId = catId });
                }
                await _context.SaveChangesAsync();
            }

            return game;
        }

        public async Task<Game?> UpdateAsync(string id, Game update, List<string> categoryIds)
        {
            var game = await _context.Games
                .Include(g => g.GameCategories)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (game == null) return null;

            game.Title = update.Title;
            game.Description = update.Description;
            game.Price = update.Price;
            game.ReleaseDate = update.ReleaseDate;
            game.Developer = update.Developer;
            game.CoverImageUrl = update.CoverImageUrl;
            game.TrailerUrl = update.TrailerUrl;

            _context.GameCategories.RemoveRange(game.GameCategories);
            if (categoryIds != null)
            {
                foreach (var catId in categoryIds)
                {
                    _context.GameCategories.Add(new GameCategory { GameId = game.Id, CategoryId = catId });
                }
            }

            await _context.SaveChangesAsync();
            return game;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var game = await _context.Games.FindAsync(id);
            if (game == null) return false;

            _context.Games.Remove(game);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetTotalGamesAsync()
        {
            return await _context.Games.CountAsync();
        }
    }
}
