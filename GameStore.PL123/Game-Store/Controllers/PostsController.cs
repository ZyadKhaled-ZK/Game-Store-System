using GameStore.PL.Models.Auth;

namespace GameStore.PL.Controllers;

public class PostsController : Controller
{
    private readonly IPostService _postService;

    public PostsController(IPostService postService)
    {
        _postService = postService;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string content)
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId))
            return Json(new { success = false, message = "Please login first." });

        if (string.IsNullOrWhiteSpace(content))
            return Json(new { success = false, message = "Post content is required." });

        var lastPostTime = await _postService.GetLastPostTimeAsync(userId);
        if (lastPostTime.HasValue)
        {
            var elapsed = DateTime.UtcNow - lastPostTime.Value;
            var remaining = 30 - (int)elapsed.TotalSeconds;
            if (remaining > 0)
                return Json(new { success = false, message = $"Please wait {remaining} seconds before posting again." });
        }

        var post = await _postService.CreateAsync(userId, content);
        return Json(new { success = true, id = post.Id, createdAt = post.CreatedAt.ToString("MMM dd, yyyy HH:mm") });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string postId)
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId))
            return Json(new { success = false, message = "Please login first." });

        var ok = await _postService.DeleteAsync(postId, userId);
        return Json(new { success = ok, message = ok ? "Post deleted." : "Post not found or not yours." });
    }
}
