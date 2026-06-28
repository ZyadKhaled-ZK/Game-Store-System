using System.Collections.Concurrent;

namespace GameStore.PL.Hubs;

public class ConnectionTracker
{
    private readonly ConcurrentDictionary<string, HashSet<string>> _connections = new();
    private readonly ConcurrentDictionary<string, DateTime> _lastSeen = new();

    public void UserConnected(string userId, string connectionId)
    {
        _connections.AddOrUpdate(userId,
            _ => new HashSet<string> { connectionId },
            (_, set) => { lock (set) set.Add(connectionId); return set; });
    }

    public void UserDisconnected(string userId, string connectionId)
    {
        if (_connections.TryGetValue(userId, out var set))
        {
            lock (set)
            {
                set.Remove(connectionId);
                if (set.Count == 0) _connections.TryRemove(userId, out _);
            }
        }
        _lastSeen[userId] = DateTime.UtcNow;
    }

    public bool IsOnline(string userId)
    {
        return _connections.ContainsKey(userId);
    }

    public List<string> GetOnlineUsers()
    {
        return _connections.Keys.ToList();
    }

    public HashSet<string> GetOnlineFriends(IEnumerable<string> friendIds)
    {
        var online = new HashSet<string>();
        foreach (var id in friendIds)
        {
            if (_connections.ContainsKey(id)) online.Add(id);
        }
        return online;
    }

    public DateTime? GetLastSeen(string userId)
    {
        return _lastSeen.TryGetValue(userId, out var dt) ? dt : null;
    }
}
