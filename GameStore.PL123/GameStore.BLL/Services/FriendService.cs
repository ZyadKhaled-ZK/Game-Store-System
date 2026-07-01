namespace GameStore.BLL.Services;

public class FriendService : IFriendService
{
    private readonly IUnitOfWork _uow;

    public FriendService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<List<Friendship>> GetFriendsAsync(string userId)
    {
        var friends = await _uow.Repository<Friendship>().Query()
            .Include(f => f.Requester)
            .Include(f => f.Receiver)
            .Where(f => (f.RequesterId == userId || f.ReceiverId == userId) && f.Status == FriendshipStatus.Accepted)
            .ToListAsync();

        return friends;
    }

    public async Task<List<Friendship>> GetPendingRequestsAsync(string userId)
    {
        var requests = await _uow.Repository<Friendship>().Query()
            .Include(f => f.Requester)
            .Include(f => f.Receiver)
            .Where(f => f.ReceiverId == userId && f.Status == FriendshipStatus.Pending)
            .ToListAsync();

        return requests;
    }

    public async Task<List<string>> GetFriendIdsAsync(string userId)
    {
        var sent = await _uow.Repository<Friendship>().Query()
            .Where(f => f.RequesterId == userId && f.Status == FriendshipStatus.Accepted)
            .Select(f => f.ReceiverId)
            .ToListAsync();

        var received = await _uow.Repository<Friendship>().Query()
            .Where(f => f.ReceiverId == userId && f.Status == FriendshipStatus.Accepted)
            .Select(f => f.RequesterId)
            .ToListAsync();

        var friendIds = new List<string>();
        friendIds.AddRange(sent);
        friendIds.AddRange(received);
        return friendIds;
    }

    public async Task<(bool Success, string Error)> SendRequestAsync(string requesterId, string receiverUsername)
    {
        var receiver = await _uow.Repository<User>().FirstOrDefaultAsync(u => u.Username == receiverUsername);
        if (receiver == null)
            return (false, "User not found.");

        if (receiver.Id == requesterId)
            return (false, "You cannot add yourself as a friend.");

        var existing = await _uow.Repository<Friendship>().Query()
            .Where(f =>
                (f.RequesterId == requesterId && f.ReceiverId == receiver.Id) ||
                (f.RequesterId == receiver.Id && f.ReceiverId == requesterId))
            .FirstOrDefaultAsync();

        if (existing != null)
        {
            if (existing.Status == FriendshipStatus.Accepted)
                return (false, "You are already friends with this user.");
            if (existing.Status == FriendshipStatus.Pending)
            {
                if (existing.RequesterId == requesterId)
                    return (false, "Friend request already sent.");
                existing.Status = FriendshipStatus.Accepted;
                _uow.Repository<Friendship>().Update(existing);
                await _uow.SaveChangesAsync();
                return (true, "Friend request accepted!");
            }
            if (existing.Status == FriendshipStatus.Rejected)
            {
                existing.Status = FriendshipStatus.Pending;
                _uow.Repository<Friendship>().Update(existing);
                await _uow.SaveChangesAsync();
                return (true, "Friend request sent.");
            }
        }

        var friendship = new Friendship
        {
            RequesterId = requesterId,
            ReceiverId = receiver.Id,
            Status = FriendshipStatus.Pending
        };

        await _uow.Repository<Friendship>().AddAsync(friendship);
        await _uow.SaveChangesAsync();

        return (true, "Friend request sent.");
    }

    public async Task<(bool Success, string Error)> AcceptRequestAsync(string friendshipId, string userId)
    {
        var friendship = await _uow.Repository<Friendship>().GetByIdAsync(friendshipId);
        if (friendship == null)
            return (false, "Friend request not found.");
        if (friendship.ReceiverId != userId)
            return (false, "Unauthorized.");
        if (friendship.Status != FriendshipStatus.Pending)
            return (false, "Request is no longer pending.");

        friendship.Status = FriendshipStatus.Accepted;
        _uow.Repository<Friendship>().Update(friendship);
        await _uow.SaveChangesAsync();

        return (true, "Friend request accepted!");
    }

    public async Task<(bool Success, string Error)> RejectRequestAsync(string friendshipId, string userId)
    {
        var friendship = await _uow.Repository<Friendship>().GetByIdAsync(friendshipId);
        if (friendship == null)
            return (false, "Friend request not found.");
        if (friendship.ReceiverId != userId)
            return (false, "Unauthorized.");

        friendship.Status = FriendshipStatus.Rejected;
        _uow.Repository<Friendship>().Update(friendship);
        await _uow.SaveChangesAsync();

        return (true, "Friend request rejected.");
    }

    public async Task<(bool Success, string Error)> RemoveFriendAsync(string friendshipId, string userId)
    {
        var friendship = await _uow.Repository<Friendship>().GetByIdAsync(friendshipId);
        if (friendship == null)
            return (false, "Friendship not found.");
        if (friendship.RequesterId != userId && friendship.ReceiverId != userId)
            return (false, "Unauthorized.");

        _uow.Repository<Friendship>().Delete(friendship);
        await _uow.SaveChangesAsync();

        return (true, "Friend removed.");
    }

}
