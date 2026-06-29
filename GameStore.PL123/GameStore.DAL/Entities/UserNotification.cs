using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameStore.DAL.Entities
{
    public class UserNotification
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string? UserId { get; set; }

        public string? SenderUserId { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        public string Type { get; set; } = "info";

        public string Category { get; set; } = string.Empty;

        public string? ReferenceId { get; set; }

        public string? ReferenceUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ReadAt { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        [ForeignKey(nameof(SenderUserId))]
        public User? SenderUser { get; set; }
    }
}
