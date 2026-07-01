using Microsoft.AspNetCore.SignalR;

namespace GameStore.PL.Hubs;

public class NotificationHub : Hub // TECHNOLOGY: SignalR Hub - Real-time notifications + chat
{
    private readonly ConnectionTracker _tracker;
    private readonly IChatService _chatService;

    public NotificationHub(ConnectionTracker tracker, IChatService chatService)
    {
        _tracker = tracker;
        _chatService = chatService;
    }

    public async Task JoinUserGroup(string userId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, userId);
    }

    public async Task JoinAdmins()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.GetHttpContext()?.Session?.GetString("UserId");
        if (!string.IsNullOrEmpty(userId))
        {
            _tracker.UserConnected(userId, Context.ConnectionId);
            await NotifyFriends(userId, true);
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.GetHttpContext()?.Session?.GetString("UserId");
        if (!string.IsNullOrEmpty(userId))
        {
            _tracker.UserDisconnected(userId, Context.ConnectionId);
            var stillOnline = _tracker.IsOnline(userId);
            if (!stillOnline)
                await NotifyFriends(userId, false);
        }
        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendMessage(string receiverId, string content)
    {
        var senderId = Context.GetHttpContext()?.Session?.GetString("UserId");
        if (string.IsNullOrEmpty(senderId) || string.IsNullOrWhiteSpace(content)) return;

        var msg = await _chatService.SendMessageAsync(senderId, receiverId, content);
        if (msg == null) return;

        var senderName = Context.GetHttpContext()?.Session?.GetString("Username") ?? "Unknown";

        await Clients.Group(receiverId).SendAsync("ReceiveMessage", new
        {
            id = msg.Id,
            senderId,
            senderName,
            receiverId,
            content = msg.Content,
            sentAt = msg.SentAt,
            readAt = (DateTime?)null
        });

        await Clients.Caller.SendAsync("MessageSent", new
        {
            id = msg.Id,
            receiverId,
            content = msg.Content,
            sentAt = msg.SentAt
        });
    }

    public async Task MarkRead(string senderId)
    {
        var userId = Context.GetHttpContext()?.Session?.GetString("UserId");
        if (string.IsNullOrEmpty(userId)) return;

        await _chatService.MarkAsReadAsync(senderId, userId);

        await Clients.Group(senderId).SendAsync("MessagesRead", new
        {
            byUserId = userId
        });
    }

    public async Task Typing(string receiverId, bool isTyping)
    {
        var senderId = Context.GetHttpContext()?.Session?.GetString("UserId");
        if (string.IsNullOrEmpty(senderId)) return;

        await Clients.Group(receiverId).SendAsync("UserTyping", new
        {
            userId = senderId,
            isTyping
        });
    }

    private async Task NotifyFriends(string userId, bool online)
    {
        await Clients.All.SendAsync(online ? "UserOnline" : "UserOffline", new
        {
            userId,
            lastSeen = online ? (DateTime?)null : DateTime.UtcNow
        });
    }
}
