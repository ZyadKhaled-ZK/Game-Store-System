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
        public DbSet<WishlistItem>  WishlistItems  { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
        public DbSet<Developer> Developers { get; set; }
        public DbSet<DeveloperApplication> DeveloperApplications { get; set; }
        public DbSet<Friendship> Friendships { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<Sale> Sales { get; set; }
        public DbSet<UserNotification> UserNotifications { get; set; }
        public DbSet<SupportTicket> SupportTickets { get; set; }
        public DbSet<SupportTicketReply> SupportTicketReplies { get; set; }

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

                e.HasOne(g => g.DeveloperNav)
                 .WithMany(d => d.Games)
                 .HasForeignKey(g => g.DeveloperId)
                 .OnDelete(DeleteBehavior.SetNull);
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
                e.HasIndex(o => o.StripeSessionId).IsUnique().HasFilter("[StripeSessionId] IS NOT NULL");
                e.HasIndex(o => o.CreatedAt);

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

            // ── PasswordResetToken ────────────────────────────────────────
            modelBuilder.Entity<PasswordResetToken>(e =>
            {
                e.HasKey(p => p.Id);
                e.HasIndex(p => p.Token).IsUnique();

                e.HasOne(p => p.User)
                 .WithMany()
                 .HasForeignKey(p => p.UserId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ── WishlistItem ───────────────────────────────────────────────
            modelBuilder.Entity<WishlistItem>(e =>
            {
                e.HasKey(w => w.Id);
                e.HasIndex(w => new { w.UserId, w.GameId }).IsUnique();

                e.HasOne(w => w.User)
                 .WithMany(u => u.WishlistItems)
                 .HasForeignKey(w => w.UserId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(w => w.Game)
                 .WithMany(g => g.WishlistItems)
                 .HasForeignKey(w => w.GameId)
                 .OnDelete(DeleteBehavior.Cascade);
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

            // ── Developer ─────────────────────────────────────────────────────
            modelBuilder.Entity<Developer>(e =>
            {
                e.HasKey(d => d.Id);
                e.HasIndex(d => d.Slug).IsUnique().HasFilter("[Slug] IS NOT NULL");
                e.Property(d => d.Name).HasMaxLength(200).IsRequired();
                e.Property(d => d.Slug).HasMaxLength(300);
                e.Property(d => d.Description).HasMaxLength(2000);
                e.Property(d => d.Website).HasMaxLength(500);
                e.Property(d => d.LogoUrl).HasMaxLength(500);
                e.Property(d => d.Country).HasMaxLength(100);

                e.HasOne(d => d.User)
                 .WithMany()
                 .HasForeignKey(d => d.UserId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ── Friendship ─────────────────────────────────────────────────────
            modelBuilder.Entity<Friendship>(e =>
            {
                e.HasKey(f => f.Id);
                e.Property(f => f.Status).HasConversion<int>();

                e.HasOne(f => f.Requester)
                 .WithMany(u => u.SentFriendRequests)
                 .HasForeignKey(f => f.RequesterId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(f => f.Receiver)
                 .WithMany(u => u.ReceivedFriendRequests)
                 .HasForeignKey(f => f.ReceiverId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasIndex(f => new { f.RequesterId, f.ReceiverId }).IsUnique();
            });

            // ── Message ─────────────────────────────────────────────────────────
            modelBuilder.Entity<Message>(e =>
            {
                e.HasKey(m => m.Id);
                e.Property(m => m.Content).IsRequired();

                e.HasOne(m => m.Sender)
                 .WithMany(u => u.SentMessages)
                 .HasForeignKey(m => m.SenderId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(m => m.Receiver)
                 .WithMany(u => u.ReceivedMessages)
                 .HasForeignKey(m => m.ReceiverId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasIndex(m => m.SentAt);
            });

            // ── Post ─────────────────────────────────────────────────────────────
            modelBuilder.Entity<Post>(e =>
            {
                e.HasKey(p => p.Id);
                e.Property(p => p.Content).HasMaxLength(1000).IsRequired();

                e.HasOne(p => p.User)
                 .WithMany()
                 .HasForeignKey(p => p.UserId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasIndex(p => p.CreatedAt);
            });

            // ── DeveloperApplication ───────────────────────────────────────────
            modelBuilder.Entity<DeveloperApplication>(e =>
            {
                e.HasKey(a => a.Id);
                e.Property(a => a.Name).HasMaxLength(200).IsRequired();
                e.Property(a => a.Description).HasMaxLength(2000);
                e.Property(a => a.Website).HasMaxLength(500);
                e.Property(a => a.Country).HasMaxLength(100);
                e.Property(a => a.CvFilePath).HasMaxLength(500);
                e.Property(a => a.GithubUrl).HasMaxLength(500);
                e.Property(a => a.Status).HasConversion<int>();

                e.HasOne(a => a.User)
                 .WithMany()
                 .HasForeignKey(a => a.UserId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ── Sale ─────────────────────────────────────────────────────────────
            modelBuilder.Entity<Sale>(e =>
            {
                e.HasKey(s => s.Id);
                e.Property(s => s.NewPrice).HasColumnType("decimal(10,2)").IsRequired();
                e.Property(s => s.Status).HasConversion<int>();
                e.Property(s => s.RejectReason).HasMaxLength(500);

                e.HasOne(s => s.Game)
                 .WithMany()
                 .HasForeignKey(s => s.GameId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(s => s.Developer)
                 .WithMany()
                 .HasForeignKey(s => s.DeveloperId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasIndex(s => new { s.Status, s.EndDate });
            });

            // ── UserNotification ─────────────────────────────────────────────────
            modelBuilder.Entity<UserNotification>(e =>
            {
                e.HasKey(n => n.Id);
                e.Property(n => n.Title).HasMaxLength(200).IsRequired();
                e.Property(n => n.Message).HasMaxLength(1000).IsRequired();
                e.Property(n => n.Type).HasMaxLength(20).IsRequired();
                e.Property(n => n.Category).HasMaxLength(50).IsRequired();
                e.Property(n => n.ReferenceUrl).HasMaxLength(500);
                e.HasIndex(n => n.UserId);
                e.HasIndex(n => n.CreatedAt);

                e.HasOne(n => n.User)
                 .WithMany()
                 .HasForeignKey(n => n.UserId)
                 .OnDelete(DeleteBehavior.NoAction);

                e.HasOne(n => n.SenderUser)
                 .WithMany()
                 .HasForeignKey(n => n.SenderUserId)
                 .OnDelete(DeleteBehavior.NoAction);
            });

            // ── SupportTicket ─────────────────────────────────────────────────────
            modelBuilder.Entity<SupportTicket>(e =>
            {
                e.HasKey(t => t.Id);
                e.Property(t => t.Subject).HasMaxLength(200).IsRequired();
                e.Property(t => t.Message).HasMaxLength(2000).IsRequired();
                e.Property(t => t.Email).HasMaxLength(200);
                e.Property(t => t.Status).HasConversion<int>();

                e.HasOne(t => t.User)
                 .WithMany()
                 .HasForeignKey(t => t.UserId)
                 .OnDelete(DeleteBehavior.SetNull);

                e.HasIndex(t => t.CreatedAt);
            });

            // ── SupportTicketReply ─────────────────────────────────────────────────
            modelBuilder.Entity<SupportTicketReply>(e =>
            {
                e.HasKey(r => r.Id);
                e.Property(r => r.Message).HasMaxLength(2000).IsRequired();

                e.HasOne(r => r.Ticket)
                 .WithMany(t => t.Replies)
                 .HasForeignKey(r => r.TicketId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(r => r.User)
                 .WithMany()
                 .HasForeignKey(r => r.UserId)
                 .OnDelete(DeleteBehavior.SetNull);

                e.HasIndex(r => r.TicketId);
                e.HasIndex(r => r.CreatedAt);
            });
        }
    }
}
