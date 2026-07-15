namespace GameStore.PL.Services;

public interface IJsonFileStore
{
    Task<T?> ReadAsync<T>(string relativePath) where T : class;
    Task WriteAsync<T>(string relativePath, T data) where T : class;
    Task DeleteAsync(string relativePath);
    bool Exists(string relativePath);
}
