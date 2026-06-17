namespace GameStore.DAL.DataBase
{
    public class GameStoreDbContext : DbContext
    {
        public GameStoreDbContext(DbContextOptions<GameStoreDbContext> options)
            : base(options) { }

        // ─── DbSets ────────────────────────────────────────────────────────────
        public DbSet<User>          Users          { get; set; }
        public DbSet<Game>          Games          { get; set; }
        public DbSet<Category>      Categories     { get; set; }
        public DbSet<GameCategory>  GameCategories { get; set; }
        public DbSet<Cart>          Carts          { get; set; }
        public DbSet<CartItem>      CartItems      { get; set; }
        public DbSet<Order>         Orders         { get; set; }
        public DbSet<OrderItem>     OrderItems     { get; set; }
        public DbSet<Library>       Libraries      { get; set; }
        public DbSet<LibraryGame>   LibraryGames   { get; set; }
        public DbSet<Review>        Reviews        { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ── User ──────────────────────────────────────────────────────────
            modelBuilder.Entity<User>(e =>
            {
                e.HasKey(u => u.Id);
                e.HasIndex(u => u.Email).IsUnique();
                e.Property(u => u.Username).HasMaxLength(100).IsRequired();
                e.Property(u => u.Email).HasMaxLength(200).IsRequired();
                e.Property(u => u.PasswordHash).IsRequired();
                e.Property(u => u.Role).HasConversion<int>();
            });

            // ── Game ──────────────────────────────────────────────────────────
            modelBuilder.Entity<Game>(e =>
            {
                e.HasKey(g => g.Id);
                e.Property(g => g.Title).HasMaxLength(200).IsRequired();
                e.Property(g => g.Developer).HasMaxLength(200);
                e.Property(g => g.Price).HasColumnType("decimal(10,2)");
                e.Property(g => g.GameFileName).HasMaxLength(260);
                // Store ScreenshotUrls list as JSON array string
                e.Property(g => g.ScreenshotUrls)
                 .HasConversion(
                     v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                     v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>()
                 )
                 .HasColumnType("nvarchar(max)");
            });

            // ── Category ─────────────────────────────────────────────────────
            modelBuilder.Entity<Category>(e =>
            {
                e.HasKey(c => c.Id);
                e.HasIndex(c => c.Name).IsUnique();
                e.Property(c => c.Name).HasMaxLength(100).IsRequired();
            });

            // ── GameCategory (many-to-many join) ─────────────────────────────
            modelBuilder.Entity<GameCategory>(e =>
            {
                e.HasKey(gc => new { gc.GameId, gc.CategoryId });

                e.HasOne(gc => gc.Game)
                 .WithMany(g => g.GameCategories)
                 .HasForeignKey(gc => gc.GameId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(gc => gc.Category)
                 .WithMany(c => c.GameCategories)
                 .HasForeignKey(gc => gc.CategoryId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ── Cart ─────────────────────────────────────────────────────────
            modelBuilder.Entity<Cart>(e =>
            {
                e.HasKey(c => c.Id);
                e.HasIndex(c => c.UserId).IsUnique(); // one cart per user

                e.HasOne(c => c.User)
                 .WithOne(u => u.Cart)
                 .HasForeignKey<Cart>(c => c.UserId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ── CartItem ─────────────────────────────────────────────────────
            modelBuilder.Entity<CartItem>(e =>
            {
                e.HasKey(ci => ci.Id);
                e.HasIndex(ci => new { ci.CartId, ci.GameId }).IsUnique(); // no duplicate games in cart

                e.HasOne(ci => ci.Cart)
                 .WithMany(c => c.CartItems)
                 .HasForeignKey(ci => ci.CartId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(ci => ci.Game)
                 .WithMany(g => g.CartItems)
                 .HasForeignKey(ci => ci.GameId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ── Order ─────────────────────────────────────────────────────────
            modelBuilder.Entity<Order>(e =>
            {
                e.HasKey(o => o.Id);
                e.Property(o => o.TotalPrice).HasColumnType("decimal(10,2)");
                e.Property(o => o.Status).HasConversion<int>();

                e.HasOne(o => o.User)
                 .WithMany(u => u.Orders)
                 .HasForeignKey(o => o.UserId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ── OrderItem ────────────────────────────────────────────────────
            modelBuilder.Entity<OrderItem>(e =>
            {
                e.HasKey(oi => oi.Id);
                e.Property(oi => oi.PriceAtPurchase).HasColumnType("decimal(10,2)");

                e.HasOne(oi => oi.Order)
                 .WithMany(o => o.OrderItems)
                 .HasForeignKey(oi => oi.OrderId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(oi => oi.Game)
                 .WithMany(g => g.OrderItems)
                 .HasForeignKey(oi => oi.GameId)
                 .OnDelete(DeleteBehavior.Restrict); // keep game record in history
            });

            // ── Library ──────────────────────────────────────────────────────
            modelBuilder.Entity<Library>(e =>
            {
                e.HasKey(l => l.Id);
                e.HasIndex(l => l.UserId).IsUnique(); // one library per user

                e.HasOne(l => l.User)
                 .WithOne(u => u.Library)
                 .HasForeignKey<Library>(l => l.UserId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ── LibraryGame (many-to-many join) ──────────────────────────────
            modelBuilder.Entity<LibraryGame>(e =>
            {
                e.HasKey(lg => new { lg.LibraryId, lg.GameId });

                e.HasOne(lg => lg.Library)
                 .WithMany(l => l.LibraryGames)
                 .HasForeignKey(lg => lg.LibraryId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(lg => lg.Game)
                 .WithMany(g => g.LibraryGames)
                 .HasForeignKey(lg => lg.GameId)
                 .OnDelete(DeleteBehavior.Restrict); // keep game in libraries even if somehow detached
            });

            // ── Review ───────────────────────────────────────────────────────
            modelBuilder.Entity<Review>(e =>
            {
                e.HasKey(r => r.Id);
                e.HasIndex(r => new { r.UserId, r.GameId }).IsUnique(); // one review per user per game
                e.Property(r => r.Rating).IsRequired();

                e.HasOne(r => r.User)
                 .WithMany(u => u.Reviews)
                 .HasForeignKey(r => r.UserId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(r => r.Game)
                 .WithMany(g => g.Reviews)
                 .HasForeignKey(r => r.GameId)
                 .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
