namespace GameStore.BLL.Services
{
    public interface IGameFileService
    {
        Task UpdateGameFileAsync(string id, string? fileUrl, string? fileName, long fileSize);
        Task ClearGameFileAsync(string id);
        Task AddScreenshotAsync(string gameId, string url);
        Task RemoveScreenshotAsync(string gameId, string url);
    }
}
