namespace GameStore.DAL.Entities
{
    public class Library
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string UserId { get; set; } = string.Empty;

        // Navigation Properties
        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;

        public ICollection<LibraryGame> LibraryGames { get; set; } = new List<LibraryGame>();
    }
}
