namespace GameStore.PL.Services;

public class GameVersionModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string VersionLabel { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string? Changelog { get; set; }
    public bool IsCurrent { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}

public interface IGameVersionService
{
    Task<List<GameVersionModel>> GetAllAsync(string gameId);
    Task<GameVersionModel?> GetLatestAsync(string gameId);
    string? GetFilePath(string gameId, GameVersionModel version);
    Task<(bool Success, string Message)> CreateAsync(string gameId, string versionLabel, string changelog, IFormFile file);
    Task<(bool Success, string Message)> DeleteAsync(string gameId, string versionId);
    Task<(bool Success, string Message)> SetCurrentAsync(string gameId, string versionId);
}
