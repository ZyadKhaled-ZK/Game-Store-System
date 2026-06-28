namespace GameStore.DAL.Entities;

public class Message
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public string SenderId { get; set; } = string.Empty;

    [Required]
    public string ReceiverId { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    public DateTime? ReadAt { get; set; }

    public User Sender { get; set; } = null!;
    public User Receiver { get; set; } = null!;
}
