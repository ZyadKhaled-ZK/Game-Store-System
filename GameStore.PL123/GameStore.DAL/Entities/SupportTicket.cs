using System.ComponentModel.DataAnnotations;
using GameStore.DAL.Enum;

namespace GameStore.DAL.Entities;

public class SupportTicket
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string? UserId { get; set; }

    [MaxLength(200)]
    public string? Email { get; set; }

    [Required, MaxLength(200)]
    public string Subject { get; set; } = string.Empty;

    [Required, MaxLength(2000)]
    public string Message { get; set; } = string.Empty;

    public TicketStatus Status { get; set; } = TicketStatus.Open;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }

    public List<SupportTicketReply> Replies { get; set; } = new();
}
