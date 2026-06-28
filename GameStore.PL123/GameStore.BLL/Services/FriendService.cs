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

    public async Task<List<(User User, int MutualGamesCount)>> GetSuggestionsAsync(string userId, int count = 6)
    {
        var friendIds = await GetFriendIdsAsync(userId);
        var pendingIds = await _uow.Repository<Friendship>().Query()
            .Where(f => f.RequesterId == userId || f.ReceiverId == userId)
            .Select(f => f.RequesterId == userId ? f.ReceiverId : f.RequesterId)
            .Distinct().ToListAsync();

        var adminIds = await _uow.Repository<User>().Query()
            .Where(u => u.Role == Role.ADMIN)
            .Select(u => u.Id).ToListAsync();

        var excludeIds = new List<string> { userId };
        excludeIds.AddRange(friendIds);
        excludeIds.AddRange(pendingIds);
        excludeIds.AddRange(adminIds);
        excludeIds = excludeIds.Distinct().ToList();

        var myLibrary = await _uow.Repository<Library>().Query()
            .Include(l => l.LibraryGames)
            .FirstOrDefaultAsync(l => l.UserId == userId);
        if (myLibrary?.LibraryGames == null || myLibrary.LibraryGames.Count == 0)
            return new List<(User User, int MutualGamesCount)>();

        var myGameIds = myLibrary.LibraryGames.Select(lg => lg.GameId).ToHashSet();

        var myCategoryIds = await _uow.Repository<GameCategory>().Query()
            .Where(gc => myGameIds.Contains(gc.GameId))
            .Select(gc => gc.CategoryId).Distinct().ToListAsync();
        var myCategorySet = myCategoryIds.ToHashSet();

        var myDeveloperIds = await _uow.Repository<Game>().Query()
            .Where(g => myGameIds.Contains(g.Id))
            .Select(g => g.DeveloperId).Distinct().ToListAsync();
        var myDeveloperSet = myDeveloperIds.ToHashSet();

        var myWishlistIds = await _uow.Repository<WishlistItem>().Query()
            .Where(w => w.UserId == userId)
            .Select(w => w.GameId).ToListAsync();
        var myWishlistSet = myWishlistIds.ToHashSet();

        var candidates = await _uow.Repository<Library>().Query()
            .Include(l => l.User)
            .Include(l => l.LibraryGames)
            .Where(l => !excludeIds.Contains(l.UserId) &&
                        l.LibraryGames.Any(lg => myGameIds.Contains(lg.GameId)))
            .ToListAsync();

        if (candidates.Count == 0)
            return new List<(User User, int MutualGamesCount)>();

        var candidateUserIds = candidates.Select(c => c.UserId).ToHashSet();
        var candidateAllGameIds = candidates.SelectMany(c => c.LibraryGames.Select(lg => lg.GameId)).Distinct().ToList();

        var candidateGameCategories = await _uow.Repository<GameCategory>().Query()
            .Where(gc => candidateAllGameIds.Contains(gc.GameId))
            .ToListAsync();
        var gameCategoryMap = candidateGameCategories.GroupBy(gc => gc.GameId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.CategoryId).ToHashSet());

        var candidateGames = await _uow.Repository<Game>().Query()
            .Where(g => candidateAllGameIds.Contains(g.Id))
            .Select(g => new { g.Id, g.DeveloperId })
            .ToListAsync();
        var gameDeveloperMap = candidateGames.ToDictionary(g => g.Id, g => g.DeveloperId);

        var candidateWishlists = await _uow.Repository<WishlistItem>().Query()
            .Where(w => candidateUserIds.Contains(w.UserId))
            .ToListAsync();
        var wishlistByUser = candidateWishlists.GroupBy(w => w.UserId)
            .ToDictionary(g => g.Key, g => g.Select(w => w.GameId).ToHashSet());

        var allFriendLinks = await _uow.Repository<Friendship>().Query()
            .Where(f => f.Status == FriendshipStatus.Accepted &&
                        (candidateUserIds.Contains(f.RequesterId) || candidateUserIds.Contains(f.ReceiverId)))
            .Select(f => new { f.RequesterId, f.ReceiverId })
            .ToListAsync();
        var candidateFriendMap = new Dictionary<string, HashSet<string>>();
        foreach (var cid in candidateUserIds)
        {
            var ids = new HashSet<string>();
            foreach (var link in allFriendLinks)
            {
                if (link.RequesterId == cid) ids.Add(link.ReceiverId);
                if (link.ReceiverId == cid) ids.Add(link.RequesterId);
            }
            candidateFriendMap[cid] = ids;
        }

        var results = new List<(User User, int MutualGamesCount, double Score)>();
        var rng = new Random();

        foreach (var lib in candidates)
        {
            var cUserId = lib.UserId;
            var cGameIds = lib.LibraryGames.Select(lg => lg.GameId).ToHashSet();

            var sharedGameIds = cGameIds.Where(id => myGameIds.Contains(id)).ToHashSet();
            int mutualGames = sharedGameIds.Count;

            var catCount = sharedGameIds
                .Where(id => gameCategoryMap.ContainsKey(id))
                .SelectMany(id => gameCategoryMap[id])
                .Where(catId => myCategorySet.Contains(catId))
                .Distinct().Count();

            var devCount = sharedGameIds
                .Where(id => gameDeveloperMap.ContainsKey(id))
                .Select(id => gameDeveloperMap[id])
                .Where(dId => myDeveloperSet.Contains(dId))
                .Distinct().Count();

            var wishCount = myWishlistIds.Count(id =>
                wishlistByUser.TryGetValue(cUserId, out var wSet) && wSet.Contains(id));

            var mutualFriendCount = candidateFriendMap.TryGetValue(cUserId, out var cfSet)
                ? friendIds.Count(id => cfSet.Contains(id))
                : 0;

            double score = mutualGames * 10.0
                         + catCount * 3.0
                         + devCount * 4.0
                         + wishCount * 5.0
                         + mutualFriendCount * 7.0;

            results.Add((lib.User, mutualGames, score));
        }

        return results
            .OrderByDescending(r => r.Score)
            .ThenBy(_ => rng.Next())
            .Take(count)
            .Select(r => (r.User, r.MutualGamesCount))
            .ToList();
    }
}
