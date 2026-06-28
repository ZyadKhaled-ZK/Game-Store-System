namespace GameStore.BLL.Services;

public class ChatService : IChatService
{
    private readonly IUnitOfWork _uow;

    public ChatService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Message?> SendMessageAsync(string senderId, string receiverId, string content)
    {
        var message = new Message
        {
            SenderId = senderId,
            ReceiverId = receiverId,
            Content = content,
            SentAt = DateTime.UtcNow
        };

        await _uow.Repository<Message>().AddAsync(message);
        await _uow.SaveChangesAsync();

        return message;
    }

    public async Task<List<Message>> GetConversationAsync(string userId1, string userId2, int page = 1, int pageSize = 50)
    {
        var messages = await _uow.Repository<Message>().Query()
            .Where(m =>
                (m.SenderId == userId1 && m.ReceiverId == userId2) ||
                (m.SenderId == userId2 && m.ReceiverId == userId1))
            .OrderByDescending(m => m.SentAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        messages.Reverse();
        return messages;
    }

    public async Task<int> GetUnreadCountAsync(string userId)
    {
        return await _uow.Repository<Message>().CountAsync(m =>
            m.ReceiverId == userId && m.ReadAt == null);
    }

    public async Task MarkAsReadAsync(string senderId, string receiverId)
    {
        var unread = await _uow.Repository<Message>().Query()
            .Where(m => m.SenderId == senderId && m.ReceiverId == receiverId && m.ReadAt == null)
            .ToListAsync();

        foreach (var msg in unread)
        {
            msg.ReadAt = DateTime.UtcNow;
            _uow.Repository<Message>().Update(msg);
        }

        if (unread.Count > 0)
            await _uow.SaveChangesAsync();
    }

    public async Task<List<(string UserId, string Username, Message? LastMessage, int UnreadCount)>> GetConversationsAsync(string userId)
    {
        var sentMessages = await _uow.Repository<Message>().Query()
            .Where(m => m.SenderId == userId)
            .Select(m => m.ReceiverId)
            .Distinct()
            .ToListAsync();

        var receivedMessages = await _uow.Repository<Message>().Query()
            .Where(m => m.ReceiverId == userId)
            .Select(m => m.SenderId)
            .Distinct()
            .ToListAsync();

        var contactIds = sentMessages.Union(receivedMessages).Distinct().ToList();
        var result = new List<(string UserId, string Username, Message? LastMessage, int UnreadCount)>();

        foreach (var contactId in contactIds)
        {
            var user = await _uow.Repository<User>().GetByIdAsync(contactId);
            if (user == null) continue;

            var lastMessage = await _uow.Repository<Message>().Query()
                .Where(m =>
                    (m.SenderId == userId && m.ReceiverId == contactId) ||
                    (m.SenderId == contactId && m.ReceiverId == userId))
                .OrderByDescending(m => m.SentAt)
                .FirstOrDefaultAsync();

            var unreadCount = await _uow.Repository<Message>().CountAsync(m =>
                m.SenderId == contactId && m.ReceiverId == userId && m.ReadAt == null);

            result.Add((user.Id, user.Username, lastMessage, unreadCount));
        }

        return result.OrderByDescending(x => x.LastMessage?.SentAt).ToList();
    }
}
