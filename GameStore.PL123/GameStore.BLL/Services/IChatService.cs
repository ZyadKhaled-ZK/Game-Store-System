namespace GameStore.BLL.Services;

public interface IChatService
{
    Task<Message?> SendMessageAsync(string senderId, string receiverId, string content);
    Task<List<Message>> GetConversationAsync(string userId1, string userId2, int page = 1, int pageSize = 50);
    Task<int> GetUnreadCountAsync(string userId);
    Task MarkAsReadAsync(string senderId, string receiverId);
    Task<List<(string UserId, string Username, Message? LastMessage, int UnreadCount)>> GetConversationsAsync(string userId);
}
