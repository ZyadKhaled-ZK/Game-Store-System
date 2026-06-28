namespace GameStore.PL.Models.Chat;

public class ChatIndexViewModel
{
    public List<ConversationViewModel> Conversations { get; set; } = new();
    public string? ActiveUserId { get; set; }
    public string? ActiveUsername { get; set; }
    public List<MessageViewModel> Messages { get; set; } = new();
    public int UnreadCount { get; set; }
}

public class ConversationViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? LastMessage { get; set; }
    public DateTime? LastMessageAt { get; set; }
    public bool IsOnline { get; set; }
    public int UnreadCount { get; set; }
}

public class MessageViewModel
{
    public string Id { get; set; } = string.Empty;
    public string SenderId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public bool IsMine { get; set; }
}
