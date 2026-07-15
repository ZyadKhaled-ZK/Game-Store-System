using System.Text.Json;

namespace GameStore.PL.Services;

public class GameVersionService : IGameVersionService
{
    private readonly IWebHostEnvironment _env;
    private static readonly JsonSerializerOptions _json = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private const int MaxVersions = 5;
    private const string VersionsRoot = "data\\versions";

    public GameVersionService(IWebHostEnvironment env)
    {
        _env = env;
    }

    private string ManifestPath(string gameId) =>
        Path.Combine(_env.WebRootPath, VersionsRoot, $"{gameId}.json");

    private string VersionsDir(string gameId) =>
        Path.Combine(_env.WebRootPath, VersionsRoot, gameId);

    private string VersionDir(string gameId, string versionId) =>
        Path.Combine(VersionsDir(gameId), versionId);

    private async Task<List<GameVersionModel>> ReadManifestAsync(string gameId)
    {
        var path = ManifestPath(gameId);
        if (!File.Exists(path)) return new();
        var json = await File.ReadAllTextAsync(path);
        return JsonSerializer.Deserialize<List<GameVersionModel>>(json, _json) ?? new();
    }

    private async Task WriteManifestAsync(string gameId, List<GameVersionModel> versions)
    {
        var path = ManifestPath(gameId);
        var dir = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(dir);
        var json = JsonSerializer.Serialize(versions, _json);
        await File.WriteAllTextAsync(path, json);
    }

    public async Task<List<GameVersionModel>> GetAllAsync(string gameId)
    {
        return await ReadManifestAsync(gameId);
    }

    public async Task<GameVersionModel?> GetLatestAsync(string gameId)
    {
        var versions = await ReadManifestAsync(gameId);
        return versions.FirstOrDefault(v => v.IsCurrent)
            ?? versions.OrderByDescending(v => v.UploadedAt).FirstOrDefault();
    }

    public string? GetFilePath(string gameId, GameVersionModel version)
    {
        var path = Path.Combine(VersionDir(gameId, version.Id), version.StoredFileName);
        return File.Exists(path) ? path : null;
    }

    public async Task<(bool Success, string Message)> CreateAsync(string gameId, string versionLabel, string changelog, IFormFile file)
    {
        try
        {
            var versions = await ReadManifestAsync(gameId);

            var versionId = Guid.NewGuid().ToString();
            var ext = Path.GetExtension(file.FileName);
            var storedName = $"{Guid.NewGuid()}{ext}";

            var versionDir = VersionDir(gameId, versionId);
            Directory.CreateDirectory(versionDir);

            var filePath = Path.Combine(versionDir, storedName);
            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var isFirst = !versions.Any();
            var model = new GameVersionModel
            {
                Id = versionId,
                VersionLabel = versionLabel,
                FileName = file.FileName,
                StoredFileName = storedName,
                FileSizeBytes = file.Length,
                Changelog = changelog,
                IsCurrent = isFirst,
                UploadedAt = DateTime.UtcNow
            };

            versions.Add(model);

            // Enforce MaxVersions: remove oldest non-current versions
            var current = versions.FirstOrDefault(v => v.IsCurrent);
            while (versions.Count > MaxVersions)
            {
                var oldest = versions.Where(v => !v.IsCurrent)
                    .OrderBy(v => v.UploadedAt).FirstOrDefault();
                if (oldest == null) break;

                var delDir = VersionDir(gameId, oldest.Id);
                if (Directory.Exists(delDir))
                    Directory.Delete(delDir, recursive: true);

                versions.Remove(oldest);
            }

            await WriteManifestAsync(gameId, versions);
            return (true, $"Version {versionLabel} uploaded.");
        }
        catch (Exception ex)
        {
            return (false, $"Upload failed: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> DeleteAsync(string gameId, string versionId)
    {
        var versions = await ReadManifestAsync(gameId);
        var version = versions.FirstOrDefault(v => v.Id == versionId);
        if (version == null) return (false, "Version not found.");

        if (version.IsCurrent && versions.Count > 1)
            return (false, "Cannot delete the current version. Set another version as current first.");

        var delDir = VersionDir(gameId, versionId);
        if (Directory.Exists(delDir))
            Directory.Delete(delDir, recursive: true);

        versions.Remove(version);
        await WriteManifestAsync(gameId, versions);
        return (true, $"Version {version.VersionLabel} deleted.");
    }

    public async Task<(bool Success, string Message)> SetCurrentAsync(string gameId, string versionId)
    {
        var versions = await ReadManifestAsync(gameId);
        var version = versions.FirstOrDefault(v => v.Id == versionId);
        if (version == null) return (false, "Version not found.");

        foreach (var v in versions)
            v.IsCurrent = v.Id == versionId;

        await WriteManifestAsync(gameId, versions);
        return (true, $"Version {version.VersionLabel} set as current.");
    }
}
