using Microsoft.EntityFrameworkCore;

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
        if (contactIds.Count == 0)
            return new();

        // Batch: load all contact users in one query
        var users = await _uow.Repository<User>().Query()
            .Where(u => contactIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id);

        // Batch: last message per contact using grouped query
        var lastMessages = await _uow.Repository<Message>().Query()
            .Where(m => m.SenderId == userId || m.ReceiverId == userId)
            .GroupBy(m => m.SenderId == userId ? m.ReceiverId : m.SenderId)
            .Select(g => new
            {
                ContactId = g.Key,
                LastMessage = g.OrderByDescending(m => m.SentAt).FirstOrDefault()
            })
            .ToListAsync();

        var lastMsgMap = lastMessages.ToDictionary(x => x.ContactId, x => x.LastMessage);

        // Batch: unread counts per sender
        var unreadCounts = await _uow.Repository<Message>().Query()
            .Where(m => m.ReceiverId == userId && m.ReadAt == null)
            .GroupBy(m => m.SenderId)
            .Select(g => new { SenderId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.SenderId, x => x.Count);

        var result = contactIds
            .Where(contactId => users.ContainsKey(contactId))
            .Select(contactId =>
            {
                var user = users[contactId];
                lastMsgMap.TryGetValue(contactId, out var lastMessage);
                unreadCounts.TryGetValue(contactId, out var uc);
                return (UserId: user.Id, Username: user.Username, LastMessage: lastMessage, UnreadCount: uc);
            })
            .ToList();

        return result.OrderByDescending(x => x.LastMessage?.SentAt).ToList();
    }
}
