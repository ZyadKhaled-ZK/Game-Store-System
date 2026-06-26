namespace GameStore.DAL.Entities
{
    public class WishlistItem
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public string GameId { get; set; } = string.Empty;

        public DateTime AddedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;

        [ForeignKey(nameof(GameId))]
        public Game Game { get; set; } = null!;
    }
}
