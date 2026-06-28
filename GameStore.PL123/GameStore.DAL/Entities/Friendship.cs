namespace GameStore.DAL.Entities;

public enum FriendshipStatus
{
    Pending,
    Accepted,
    Rejected
}

public class Friendship
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public string RequesterId { get; set; } = string.Empty;

    [Required]
    public string ReceiverId { get; set; } = string.Empty;

    public FriendshipStatus Status { get; set; } = FriendshipStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User Requester { get; set; } = null!;
    public User Receiver { get; set; } = null!;
}
