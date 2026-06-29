using System.ComponentModel.DataAnnotations;

namespace GameStore.DAL.Entities;

public class SupportTicketReply
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public string TicketId { get; set; } = string.Empty;

    public string? UserId { get; set; }

    [Required, MaxLength(2000)]
    public string Message { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public SupportTicket Ticket { get; set; } = null!;

    public User? User { get; set; }
}
