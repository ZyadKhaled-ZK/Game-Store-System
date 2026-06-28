using GameStore.PL.Models.Chat;

namespace GameStore.PL.Controllers;

public class ChatController : Controller
{
    private readonly IChatService _chatService;
    private readonly IFriendService _friendService;
    private readonly ConnectionTracker _tracker;

    public ChatController(IChatService chatService, IFriendService friendService,
        ConnectionTracker tracker)
    {
        _chatService = chatService;
        _friendService = friendService;
        _tracker = tracker;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? userId)
    {
        var currentUserId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(currentUserId))
            return RedirectToAction("Login", "Auth");

        var conversations = await _chatService.GetConversationsAsync(currentUserId);
        var friendIds = await _friendService.GetFriendIdsAsync(currentUserId);

        var model = new ChatIndexViewModel
        {
            Conversations = conversations.Select(c => new ConversationViewModel
            {
                UserId = c.UserId,
                Username = c.Username,
                LastMessage = c.LastMessage?.Content,
                LastMessageAt = c.LastMessage?.SentAt,
                IsOnline = _tracker.IsOnline(c.UserId),
                UnreadCount = c.UnreadCount
            }).ToList(),
            UnreadCount = await _chatService.GetUnreadCountAsync(currentUserId)
        };

        if (!string.IsNullOrEmpty(userId) && userId != currentUserId)
        {
            var user = await _chatService.GetConversationsAsync(currentUserId);
            var contact = user.FirstOrDefault(c => c.UserId == userId);
            if (contact.UserId != null)
            {
                model.ActiveUserId = userId;
                model.ActiveUsername = contact.Username;

                var messages = await _chatService.GetConversationAsync(currentUserId, userId);
                model.Messages = messages.Select(m => new MessageViewModel
                {
                    Id = m.Id,
                    SenderId = m.SenderId,
                    Content = m.Content,
                    SentAt = m.SentAt,
                    ReadAt = m.ReadAt,
                    IsMine = m.SenderId == currentUserId
                }).ToList();
            }
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> GetMessages(string userId, int page = 1)
    {
        var currentUserId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(currentUserId))
            return Json(new { messages = new List<object>() });

        var messages = await _chatService.GetConversationAsync(currentUserId, userId, page);

        var result = messages.Select(m => new
        {
            id = m.Id,
            senderId = m.SenderId,
            content = m.Content,
            sentAt = m.SentAt,
            readAt = m.ReadAt,
            isMine = m.SenderId == currentUserId
        }).ToList();

        return Json(new { messages = result });
    }

    [HttpGet]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId))
            return Json(new { count = 0 });

        var count = await _chatService.GetUnreadCountAsync(userId);
        return Json(new { count });
    }
}
