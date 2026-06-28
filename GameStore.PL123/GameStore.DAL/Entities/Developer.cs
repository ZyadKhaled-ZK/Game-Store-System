namespace GameStore.DAL.Entities
{
    public class Developer
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? Slug { get; set; }

        [MaxLength(2000)]
        public string? Description { get; set; }

        [MaxLength(500)]
        public string? Website { get; set; }

        [MaxLength(500)]
        public string? LogoUrl { get; set; }

        [MaxLength(100)]
        public string? Country { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User User { get; set; } = null!;
        public ICollection<Game> Games { get; set; } = new List<Game>();
    }
}
