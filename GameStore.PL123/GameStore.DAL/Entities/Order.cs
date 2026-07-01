
namespace GameStore.DAL.Entities
{
    public class Order
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string UserId { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required, Column(TypeName = "decimal(10,2)")]
        public decimal TotalPrice { get; set; }

        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

        [MaxLength(500)]
        public string? StripeSessionId { get; set; }

        [MaxLength(500)]
        public string? StripePaymentIntentId { get; set; }

        // ── Navigation Properties ─────────────────────────────────────────
        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;

        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
