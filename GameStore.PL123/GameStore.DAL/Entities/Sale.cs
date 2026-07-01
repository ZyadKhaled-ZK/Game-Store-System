
namespace GameStore.DAL.Entities
{
    public class Sale
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string GameId { get; set; } = string.Empty;

        [Required]
        public string DeveloperId { get; set; } = string.Empty;

        [Required, Column(TypeName = "decimal(10,2)")]
        public decimal NewPrice { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public SaleStatus Status { get; set; } = SaleStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ApprovedAt { get; set; }

        public string? ApprovedByAdminId { get; set; }

        public string? RejectReason { get; set; }

        public bool IsActive =>
            Status == SaleStatus.Approved &&
            DateTime.UtcNow >= StartDate &&
            DateTime.UtcNow <= EndDate;

        [ForeignKey(nameof(GameId))]
        public Game Game { get; set; } = null!;

        [ForeignKey(nameof(DeveloperId))]
        public Developer Developer { get; set; } = null!;
    }
}
