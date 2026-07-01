using GameStore.PL.Hubs;
using GameStore.PL.Models.Friends;
using GameStore.PL.Services;
using GameStore.DAL.Entities;
using GameStore.DAL.Enum;
using GameStore.DAL.Repo;
using Microsoft.EntityFrameworkCore;

namespace GameStore.PL.Controllers;

public class FriendsController : Controller
{
    private readonly IFriendService _friendService;
    private readonly IFriendSuggestionService _friendSuggestionService;
    private readonly IUserService _userService;
    private readonly IChatService _chatService;
    private readonly ConnectionTracker _tracker;
    private readonly INotificationService _notifService;
    private readonly IUnitOfWork _uow;

    public FriendsController(IFriendService friendService,
        IFriendSuggestionService friendSuggestionService,
        IUserService userService,
        IChatService chatService, ConnectionTracker tracker,
        INotificationService notifService, IUnitOfWork uow)
    {
        _friendService = friendService;
        _friendSuggestionService = friendSuggestionService;
        _userService = userService;
        _chatService = chatService;
        _tracker = tracker;
        _notifService = notifService;
        _uow = uow;
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
        var suggestions = await _friendSuggestionService.GetSuggestionsAsync(userId, 6);

        var conversations = await _chatService.GetConversationsAsync(userId);
        var unreadMap = conversations.ToDictionary(c => c.UserId, c => c.UnreadCount);

        var model = new FriendsIndexViewModel
        {
            Friends = friends.Select(f =>
            {
                var friendUser = f.RequesterId == userId ? f.Receiver : f.Requester;
                if (friendUser == null) return null;
                return new FriendViewModel
                {
                    FriendshipId = f.Id,
                    UserId = friendUser.Id,
                    Username = friendUser.Username,
                    Avatar = friendUser.AvatarUrl,
                    IsOnline = _tracker.IsOnline(friendUser.Id),
                    LastSeen = _tracker.GetLastSeen(friendUser.Id),
                    UnreadCount = unreadMap.GetValueOrDefault(friendUser.Id, 0)
                };
            }).Where(f => f != null).ToList()!,
            PendingRequests = pending.Select(r => new FriendRequestViewModel
            {
                FriendshipId = r.Id,
                RequesterId = r.Requester?.Id ?? "",
                Username = r.Requester?.Username ?? "[Deleted User]",
                Avatar = r.Requester?.AvatarUrl,
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
            RequesterId = r.Requester?.Id ?? "",
            Username = r.Requester?.Username ?? "[Deleted User]",
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
                await _notifService.SendToUserAsync(receiver.Id,
                    "Friend Request",
                    $"{HttpContext.Session.GetString("Username")} sent you a friend request.",
                    "info", category: "Friends");
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

        if (success)
        {
            var friendship = await _uow.Repository<Friendship>().Query()
                .FirstOrDefaultAsync(f => f.Id == friendshipId);
            if (friendship != null)
            {
                await _notifService.SendToUserAsync(friendship.RequesterId,
                    "Friend Request Accepted",
                    $"{HttpContext.Session.GetString("Username")} accepted your friend request.",
                    "success", category: "Friends", referenceId: friendshipId);
            }
        }

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

        var refIds = pending.Select(r => r.Id.ToString()).ToList();
        var notifs = await _uow.Repository<UserNotification>().Query()
            .Where(n => n.Category == "FriendRequest" && refIds.Contains(n.ReferenceId))
            .ToListAsync();
        var notifMap = notifs.ToDictionary(n => n.ReferenceId, n => n.Id);

        return Json(new
        {
            pendingRequests = pending.Count,
            unreadMessages = unread,
            requests = pending.Select(r => new
            {
                friendshipId = r.Id,
                notifId = notifMap.GetValueOrDefault(r.Id.ToString()),
                username = r.Requester?.Username ?? "[Deleted User]",
                avatar = r.Requester?.AvatarUrl,
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
