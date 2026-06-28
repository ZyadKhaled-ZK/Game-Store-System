namespace GameStore.PL.Models.Auth;

public class ProfileViewModel
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string NewEmail { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public IFormFile? AvatarFile { get; set; }
    public List<PostViewModel> Posts { get; set; } = new();
    public int PostCount { get; set; }
    public List<ConversationPreviewViewModel> RecentConversations { get; set; } = new();
    public string Message { get; set; } = string.Empty;
    public bool IsError { get; set; }
}

public class PostViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class ConversationPreviewViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public string? LastMessage { get; set; }
    public DateTime? LastMessageAt { get; set; }
    public int UnreadCount { get; set; }
    public bool IsOnline { get; set; }
}

public class PublicProfileViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public bool IsOnline { get; set; }
    public bool IsOwn { get; set; }
    public bool IsFriend { get; set; }
    public int PostCount { get; set; }
    public List<PostViewModel> Posts { get; set; } = new();
}
