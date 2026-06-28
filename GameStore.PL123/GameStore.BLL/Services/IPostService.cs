namespace GameStore.BLL.Services;

public interface IPostService
{
    Task<Post> CreateAsync(string userId, string content);
    Task<List<Post>> GetUserPostsAsync(string userId, int page = 1, int pageSize = 20);
    Task<bool> DeleteAsync(string postId, string userId);
    Task<int> GetUserPostCountAsync(string userId);
}
