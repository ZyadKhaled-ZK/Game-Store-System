using GameStore.PL.Models.Auth;

namespace GameStore.PL.Controllers;

public class ProfileController : Controller
{
    private readonly IUserService _userService;
    private readonly IPostService _postService;
    private readonly IFriendService _friendService;
    private readonly ConnectionTracker _tracker;

    public ProfileController(IUserService userService, IPostService postService,
        IFriendService friendService, ConnectionTracker tracker)
    {
        _userService = userService;
        _postService = postService;
        _friendService = friendService;
        _tracker = tracker;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return RedirectToAction("Index", "Home");

        var user = await _userService.GetUserByUsernameAsync(username);
        if (user == null)
            return RedirectToAction("Index", "Home");

        var currentUserId = HttpContext.Session.GetString("UserId");
        bool isOwn = currentUserId == user.Id;

        var posts = await _postService.GetUserPostsAsync(user.Id);
        var postCount = await _postService.GetUserPostCountAsync(user.Id);
        bool isFriend = false;
        if (!string.IsNullOrEmpty(currentUserId) && currentUserId != user.Id)
        {
            var friendIds = await _friendService.GetFriendIdsAsync(currentUserId);
            isFriend = friendIds.Contains(user.Id);
        }

        var model = new PublicProfileViewModel
        {
            UserId = user.Id,
            Username = user.Username,
            AvatarUrl = user.AvatarUrl,
            Bio = user.Bio,
            IsOnline = _tracker.IsOnline(user.Id),
            IsOwn = isOwn,
            IsFriend = isFriend,
            PostCount = postCount,
            Posts = posts.Select(p => new PostViewModel
            {
                Id = p.Id,
                Content = p.Content,
                CreatedAt = p.CreatedAt
            }).ToList()
        };

        return View(model);
    }
}
