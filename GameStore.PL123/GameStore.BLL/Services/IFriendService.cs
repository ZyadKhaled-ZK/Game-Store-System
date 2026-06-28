namespace GameStore.BLL.Services;

public interface IFriendService
{
    Task<List<Friendship>> GetFriendsAsync(string userId);
    Task<List<Friendship>> GetPendingRequestsAsync(string userId);
    Task<List<string>> GetFriendIdsAsync(string userId);
    Task<(bool Success, string Error)> SendRequestAsync(string requesterId, string receiverUsername);
    Task<(bool Success, string Error)> AcceptRequestAsync(string friendshipId, string userId);
    Task<(bool Success, string Error)> RejectRequestAsync(string friendshipId, string userId);
    Task<(bool Success, string Error)> RemoveFriendAsync(string friendshipId, string userId);
    Task<List<(User User, int MutualGamesCount)>> GetSuggestionsAsync(string userId, int count = 6);
}
