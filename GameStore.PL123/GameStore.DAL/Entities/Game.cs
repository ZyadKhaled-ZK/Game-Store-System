namespace GameStore.DAL.Entities
{
    public class Game
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        /// <summary>Price in USD. 0 = free.</summary>
        [Required, Column(TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }

        public string? TrailerUrl { get; set; }

        public DateTime ReleaseDate { get; set; }

        [MaxLength(200)]
        public string? Developer { get; set; }

        /// <summary>Cover / hero image shown on the store card.</summary>
        public string? CoverImageUrl { get; set; }

        /// <summary>Relative path to the downloadable game file.</summary>
        public string? GameFileUrl { get; set; }

        /// <summary>Original filename displayed to users.</summary>
        [MaxLength(260)]
        public string? GameFileName { get; set; }

        /// <summary>File size in bytes for display purposes.</summary>
        public long GameFileSizeBytes { get; set; }

        /// <summary>Stored as JSON array in the DB column.</summary>
        public List<string> ScreenshotUrls { get; set; } = new();

        // ── Navigation Properties ──────────────────────────────────────────
        public ICollection<GameCategory> GameCategories { get; set; } = new List<GameCategory>();
        public ICollection<CartItem>     CartItems      { get; set; } = new List<CartItem>();
        public ICollection<OrderItem>    OrderItems     { get; set; } = new List<OrderItem>();
        public ICollection<LibraryGame>  LibraryGames   { get; set; } = new List<LibraryGame>();
        public ICollection<Review>       Reviews        { get; set; } = new List<Review>();
    }
}
