namespace GameStore.DAL.Entities
{
    public class GameCategory
    {
        [Required]
        public string GameId { get; set; } = string.Empty;

        [Required]
        public string CategoryId { get; set; } = string.Empty;

        // Navigation Properties
        [ForeignKey(nameof(GameId))]
        public Game Game { get; set; } = null!;

        [ForeignKey(nameof(CategoryId))]
        public Category Category { get; set; } = null!;
    }
}
