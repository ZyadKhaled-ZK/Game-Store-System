using Microsoft.AspNetCore.SignalR;
using GameStore.PL.Hubs;
using GameStore.PL.Models.Friends;
using GameStore.DAL.Enum;

namespace GameStore.PL.Controllers;

public class FriendsController : Controller
{
    private readonly IFriendService _friendService;
    private readonly IUserService _userService;
    private readonly IChatService _chatService;
    private readonly ConnectionTracker _tracker;
    private readonly IHubContext<NotificationHub> _hub;

    public FriendsController(IFriendService friendService, IUserService userService,
        IChatService chatService, ConnectionTracker tracker, IHubContext<NotificationHub> hub)
    {
        _friendService = friendService;
        _userService = userService;
        _chatService = chatService;
        _tracker = tracker;
        _hub = hub;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? chatUserId)
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Auth");

        ViewBag.ChatUserId = chatUserId;

        var friends = await _friendService.GetFriendsAsync(userId);
        var pending = await _friendService.GetPendingRequestsAsync(userId);
        var suggestions = await _friendService.GetSuggestionsAsync(userId, 6);

        var model = new FriendsIndexViewModel
        {
            Friends = friends.Select(f =>
            {
                var friendUser = f.RequesterId == userId ? f.Receiver : f.Requester;
                return new FriendViewModel
                {
                    FriendshipId = f.Id,
                    UserId = friendUser.Id,
                    Username = friendUser.Username,
                    Avatar = friendUser.AvatarUrl,
                    IsOnline = _tracker.IsOnline(friendUser.Id),
                    LastSeen = _tracker.GetLastSeen(friendUser.Id)
                };
            }).ToList(),
            PendingRequests = pending.Select(r => new FriendRequestViewModel
            {
                FriendshipId = r.Id,
                RequesterId = r.Requester.Id,
                Username = r.Requester.Username,
                Avatar = r.Requester.AvatarUrl,
                SentAt = r.CreatedAt
            }).ToList(),
            Suggestions = suggestions.Select(s => new SuggestionViewModel
            {
                UserId = s.User.Id,
                Username = s.User.Username,
                Avatar = s.User.AvatarUrl,
                MutualGamesCount = s.MutualGamesCount,
                IsOnline = _tracker.IsOnline(s.User.Id)
            }).ToList()
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Requests()
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Auth");

        var pending = await _friendService.GetPendingRequestsAsync(userId);

        var model = pending.Select(r => new FriendRequestViewModel
        {
            FriendshipId = r.Id,
            RequesterId = r.Requester.Id,
            Username = r.Requester.Username,
            SentAt = r.CreatedAt
        }).ToList();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendRequest(string username)
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId))
            return Json(new { success = false, message = "Please login first." });

        var (success, message) = await _friendService.SendRequestAsync(userId, username);

        if (success)
        {
            var receiver = await _userService.GetUserByUsernameAsync(username);
            if (receiver != null)
            {
                await _hub.Clients.Group(receiver.Id).SendAsync("ReceiveNotification", new
                {
                    title = "Friend Request",
                    message = $"{HttpContext.Session.GetString("Username")} sent you a friend request.",
                    type = "info"
                });
            }
        }

        return Json(new { success, message });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AcceptRequest(string friendshipId)
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId))
            return Json(new { success = false, message = "Please login first." });

        var (success, message) = await _friendService.AcceptRequestAsync(friendshipId, userId);
        return Json(new { success, message });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectRequest(string friendshipId)
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId))
            return Json(new { success = false, message = "Please login first." });

        var (success, message) = await _friendService.RejectRequestAsync(friendshipId, userId);
        return Json(new { success, message });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveFriend(string friendshipId)
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId))
            return Json(new { success = false, message = "Please login first." });

        var (success, message) = await _friendService.RemoveFriendAsync(friendshipId, userId);
        return Json(new { success, message });
    }

    [HttpGet]
    public async Task<IActionResult> GetNotificationData()
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId))
            return Json(new { pendingRequests = 0, unreadMessages = 0, requests = new List<object>() });

        var pending = await _friendService.GetPendingRequestsAsync(userId);
        var unread = await _chatService.GetUnreadCountAsync(userId);

        return Json(new
        {
            pendingRequests = pending.Count,
            unreadMessages = unread,
            requests = pending.Select(r => new
            {
                friendshipId = r.Id,
                username = r.Requester.Username,
                avatar = r.Requester.AvatarUrl,
                sentAt = r.CreatedAt.ToString("MMM dd")
            })
        });
    }

    [HttpGet]
    public async Task<IActionResult> Search(string q)
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId))
            return Json(new { results = new List<object>() });

        var users = await _userService.SearchUsersAsync(q ?? "");
        var friendIds = await _friendService.GetFriendIdsAsync(userId);

        var results = users
            .Where(u => u.Id != userId && !friendIds.Contains(u.Id))
            .Select(u => new { id = u.Id, username = u.Username, avatar = u.AvatarUrl })
            .Take(10)
            .ToList();

        return Json(new { results });
    }
}
