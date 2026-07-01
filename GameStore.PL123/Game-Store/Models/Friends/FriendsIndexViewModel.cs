namespace GameStore.PL.Models.Friends;

public class FriendsIndexViewModel
{
    public List<FriendViewModel> Friends { get; set; } = new();
    public List<FriendRequestViewModel> PendingRequests { get; set; } = new();
    public List<SuggestionViewModel> Suggestions { get; set; } = new();
    public string? SearchQuery { get; set; }
}

public class SuggestionViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public int MutualGamesCount { get; set; }
    public bool IsOnline { get; set; }
}

public class FriendViewModel
{
    public string FriendshipId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public bool IsOnline { get; set; }
    public DateTime? LastSeen { get; set; }
    public int UnreadCount { get; set; }
}

public class FriendRequestViewModel
{
    public string FriendshipId { get; set; } = string.Empty;
    public string RequesterId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public DateTime SentAt { get; set; }
}
