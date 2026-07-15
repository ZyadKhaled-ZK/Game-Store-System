namespace GameStore.DAL.Entities
{
    public class OrderItem
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string OrderId { get; set; } = string.Empty;

        [Required]
        public string GameId { get; set; } = string.Empty;

        /// <summary>Price locked at time of purchase.</summary>
        [Required, Column(TypeName = "decimal(10,2)")]
        public decimal PriceAtPurchase { get; set; }

        /// <summary>Platform commission percentage at time of purchase (default 15%).</summary>
        [Range(0, 100)]
        [Column(TypeName = "decimal(5,2)")]
        public decimal CommissionPercent { get; set; } = 15m;

        // ── Navigation Properties ─────────────────────────────────────────
        [ForeignKey(nameof(OrderId))]
        public Order Order { get; set; } = null!;

        [ForeignKey(nameof(GameId))]
        public Game Game { get; set; } = null!;
    }
}
