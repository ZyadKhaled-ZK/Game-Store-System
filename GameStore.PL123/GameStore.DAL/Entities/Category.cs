namespace GameStore.DAL.Entities
{
    public class Category
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        // Navigation Properties
        public ICollection<GameCategory> GameCategories { get; set; } = new List<GameCategory>();
    }
}
