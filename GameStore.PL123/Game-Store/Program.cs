using System.IO.Compression;
using GameStore.PL.Hubs;
using GameStore.PL.Mappings;
using GameStore.PL.Services;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// ── Upload limits (100 MB max) ─────────────────────────────────────────────
builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 104_857_600;
});
builder.WebHost.ConfigureKestrel(o =>
{
    o.Limits.MaxRequestBodySize = 104_857_600; // TECHNOLOGY: Kestrel - Web server (100 MB upload limit)
});

// ── Services ────────────────────────────────────────────────────────────────
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()); // TECHNOLOGY: ASP.NET Core MVC - Controllers + Views + Anti-forgery
});

builder.Services.AddScoped<AdminOnlyFilter>();
builder.Services.AddScoped<DeveloperOnlyFilter>();

builder.Services.AddDbContext<GameStoreDbContext>(options =>
    options.UseSqlServer( // TECHNOLOGY: EF Core 9 + SQL Server - ORM + database
        builder.Configuration.GetConnectionString("GameStoreConnection"))
        .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IGameService, GameService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IOrderAnalyticsService, OrderAnalyticsService>();
builder.Services.AddScoped<IGameFileService, GameFileService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<ILibraryService, LibraryService>();
builder.Services.AddScoped<IWishlistService, WishlistService>();
builder.Services.AddScoped<IDeveloperService, DeveloperService>();
builder.Services.AddScoped<IDeveloperApplicationService, DeveloperApplicationService>();
builder.Services.AddScoped<ISaleService, SaleService>();
builder.Services.AddScoped<SeedService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IFriendService, FriendService>();
builder.Services.AddScoped<IFriendSuggestionService, FriendSuggestionService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IPostService, PostService>();
builder.Services.AddScoped<ISupportTicketService, SupportTicketService>();
builder.Services.AddAutoMapper(typeof(MappingProfile)); // TECHNOLOGY: AutoMapper 12 - Entity → ViewModel mapping
builder.Services.AddSingleton<ConnectionTracker>();
builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("Stripe")); // TECHNOLOGY: Stripe - Payment processing
var stripeSecretKey = builder.Configuration["Stripe:SecretKey"];
if (!string.IsNullOrEmpty(stripeSecretKey))
    Stripe.StripeConfiguration.ApiKey = stripeSecretKey; // TECHNOLOGY: Stripe - API key configuration
builder.Services.AddSignalR(); // TECHNOLOGY: SignalR - Real-time hub (/hub/notifications)
builder.Services.AddHttpContextAccessor();
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins()  // same-origin only by default
      .AllowCredentials()));

// TECHNOLOGY: Brotli + Gzip response compression - reduces bandwidth 60-80%
builder.Services.AddResponseCompression(o =>
{
    o.EnableForHttps = true;
    o.Providers.Add<BrotliCompressionProvider>();
    o.Providers.Add<GzipCompressionProvider>();
    o.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(["image/svg+xml"]);
});
builder.Services.Configure<BrotliCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);
builder.Services.Configure<GzipCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);

// TECHNOLOGY: Output caching - reduces DB load on read-heavy endpoints
builder.Services.AddHealthChecks(); // TECHNOLOGY: Health checks - /health endpoint
builder.Services.AddOutputCache(o =>
{
    o.DefaultExpirationTimeSpan = TimeSpan.FromMinutes(10);
    o.AddPolicy("NoCache", b => b.NoCache());
});

// Session-based auth (8-hour idle timeout)
builder.Services.AddStackExchangeRedisCache(options => // TECHNOLOGY: Redis - Distributed session cache (StackExchange)
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis")
        ?? "localhost:6379,connectRetry=3,abortConnect=false";
});
builder.Services.AddSession(options => // TECHNOLOGY: Session Auth - Custom (no Identity)
{
    options.IdleTimeout        = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly    = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite    = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.Name        = ".GameStore.Session";
});

var app = builder.Build();

// ── Database: migrate + seed ────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<GameStoreDbContext>();

    if (app.Environment.IsDevelopment())
    {
        db.Database.Migrate();
    }
    else if (app.Configuration.GetValue<bool>("ApplyMigrations"))
    {
        db.Database.Migrate();
    }

    // SeedService uses AnyAsync() internally — idempotent, safe on every start
    var seeder = scope.ServiceProvider.GetRequiredService<SeedService>();
    await seeder.SeedAsync();
}

// ── Middleware pipeline ─────────────────────────────────────────────────────
var logger = app.Services.GetRequiredService<ILogger<Program>>();
app.Use(async (ctx, next) =>
{
    try { await next(); }
    catch (Exception ex)
    {
        logger.LogError(ex, "Unhandled exception: {Path}", ctx.Request.Path);
        if (app.Environment.IsDevelopment()) throw;
        ctx.Response.Clear();
        ctx.Response.StatusCode = 500;
        ctx.Response.Redirect("/Home/Error");
    }
});

app.UseResponseCompression(); // TECHNOLOGY: Brotli/Gzip compression
app.UseHttpsRedirection(); // TECHNOLOGY: HTTPS - Auto redirect
app.UseStaticFiles(new StaticFileOptions // TECHNOLOGY: Static files - 1-year cache for versioned assets
{
    OnPrepareResponse = ctx =>
    {
        var path = ctx.Context.Request.Path;
        // Cache fingerprinted assets aggressively; HTML never cached
        if (!path.StartsWithSegments("/css") && !path.StartsWithSegments("/js") &&
            !path.StartsWithSegments("/lib") && !path.StartsWithSegments("/images"))
            return;
        ctx.Context.Response.Headers.Append("Cache-Control", "public, max-age=31536000, immutable");
    }
});
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    ctx.Response.Headers.Append("X-Frame-Options", "DENY");
    ctx.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    ctx.Response.Headers["X-Powered-By"] = "";     // remove ASP.NET header
    ctx.Response.Headers["Server"]   = "";          // remove Kestrel header
    await next();
});
app.UseRouting();
app.UseCors(); // TECHNOLOGY: CORS - Same-origin with credentials

app.UseOutputCache(); // TECHNOLOGY: Output caching - 10 min default TTL
app.UseSession(); // TECHNOLOGY: Session - Auth state
app.UseAuthorization(); // TECHNOLOGY: Auth - Role-based filters

app.MapHealthChecks("/health"); // TECHNOLOGY: Health check - load balancer probe

app.MapHub<NotificationHub>("/hub/notifications"); // TECHNOLOGY: SignalR - Notification hub endpoint

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

