using System.Collections.Concurrent;
using System.Text.Json;

namespace GameStore.PL.Services;

public class JsonFileStore : IJsonFileStore
{
    private readonly string _rootPath;
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new(StringComparer.OrdinalIgnoreCase);
    private static readonly JsonSerializerOptions _json = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public JsonFileStore(IWebHostEnvironment env)
    {
        _rootPath = Path.Combine(env.WebRootPath, "data");
        Directory.CreateDirectory(_rootPath);
    }

    private string FullPath(string relativePath)
    {
        relativePath = relativePath.Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);
        var full = Path.Combine(_rootPath, relativePath);
        var fullDir = Path.GetDirectoryName(full)!;
        if (!fullDir.StartsWith(_rootPath, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException($"Path traversal detected: {relativePath}");
        return full;
    }

    private SemaphoreSlim GetLock(string path) =>
        _locks.GetOrAdd(path, _ => new SemaphoreSlim(1, 1));

    public async Task<T?> ReadAsync<T>(string relativePath) where T : class
    {
        var path = FullPath(relativePath);
        var lockObj = GetLock(path);
        await lockObj.WaitAsync();
        try
        {
            if (!File.Exists(path)) return null;
            var json = await File.ReadAllTextAsync(path);
            return JsonSerializer.Deserialize<T>(json, _json);
        }
        finally
        {
            lockObj.Release();
        }
    }

    public async Task WriteAsync<T>(string relativePath, T data) where T : class
    {
        var path = FullPath(relativePath);
        var dir = Path.GetDirectoryName(path)!;
        var lockObj = GetLock(path);
        await lockObj.WaitAsync();
        try
        {
            Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(data, _json);
            await File.WriteAllTextAsync(path, json);
        }
        finally
        {
            lockObj.Release();
        }
    }

    public async Task DeleteAsync(string relativePath)
    {
        var path = FullPath(relativePath);
        var lockObj = GetLock(path);
        await lockObj.WaitAsync();
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        finally
        {
            lockObj.Release();
        }
    }

    public bool Exists(string relativePath)
    {
        var path = FullPath(relativePath);
        return File.Exists(path);
    }
}
