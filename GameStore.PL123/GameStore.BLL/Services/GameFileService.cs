namespace GameStore.BLL.Services
{
    public class GameFileService : IGameFileService
    {
        private readonly IUnitOfWork _uow;

        public GameFileService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task UpdateGameFileAsync(string id, string? fileUrl, string? fileName, long fileSize)
        {
            var game = await _uow.Repository<Game>().GetByIdAsync(id);
            if (game == null) return;

            game.GameFileUrl = fileUrl;
            game.GameFileName = fileName;
            game.GameFileSizeBytes = fileSize;
            await _uow.SaveChangesAsync();
        }

        public async Task ClearGameFileAsync(string id)
        {
            var game = await _uow.Repository<Game>().GetByIdAsync(id);
            if (game == null) return;

            game.GameFileUrl = null;
            game.GameFileName = null;
            game.GameFileSizeBytes = 0;
            await _uow.SaveChangesAsync();
        }

        public async Task AddScreenshotAsync(string gameId, string url)
        {
            var game = await _uow.Repository<Game>().GetByIdAsync(gameId);
            if (game == null) return;

            if (!game.ScreenshotUrls.Contains(url))
            {
                game.ScreenshotUrls.Add(url);
                await _uow.SaveChangesAsync();
            }
        }

        public async Task RemoveScreenshotAsync(string gameId, string url)
        {
            var game = await _uow.Repository<Game>().GetByIdAsync(gameId);
            if (game == null) return;

            game.ScreenshotUrls.Remove(url);
            await _uow.SaveChangesAsync();
        }
    }
}
