namespace GameStore.DAL.Entities
{
    public class LibraryGame
    {
        [Required]
        public string LibraryId { get; set; } = string.Empty;

        [Required]
        public string GameId { get; set; } = string.Empty;

        public DateTime AddedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey(nameof(LibraryId))]
        public Library Library { get; set; } = null!;

        [ForeignKey(nameof(GameId))]
        public Game Game { get; set; } = null!;
    }
}
