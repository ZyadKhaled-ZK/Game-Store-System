using Microsoft.EntityFrameworkCore;

namespace GameStore.BLL.Services;

public class PostService : IPostService
{
    private readonly IUnitOfWork _uow;

    public PostService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Post> CreateAsync(string userId, string content)
    {
        var post = new Post
        {
            UserId = userId,
            Content = content
        };

        await _uow.Repository<Post>().AddAsync(post);
        await _uow.SaveChangesAsync();
        return post;
    }

    public async Task<List<Post>> GetUserPostsAsync(string userId, int page = 1, int pageSize = 20)
    {
        return await _uow.Repository<Post>().Query()
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<bool> DeleteAsync(string postId, string userId)
    {
        var post = await _uow.Repository<Post>().GetByIdAsync(postId);
        if (post == null || post.UserId != userId)
            return false;

        _uow.Repository<Post>().Delete(post);
        await _uow.SaveChangesAsync();
        return true;
    }

    public async Task<int> GetUserPostCountAsync(string userId)
    {
        return await _uow.Repository<Post>().Query()
            .Where(p => p.UserId == userId)
            .CountAsync();
    }

    public async Task<DateTime?> GetLastPostTimeAsync(string userId)
    {
        return await _uow.Repository<Post>().Query()
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => (DateTime?)p.CreatedAt)
            .FirstOrDefaultAsync();
    }
}
