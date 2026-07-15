using System.IO.Compression;
using System.Security.Claims;
using System.Threading.RateLimiting;
using GameStore.PL.Hubs;
using GameStore.PL.Mappings;
using GameStore.PL.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
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
builder.Services.AddScoped<IGameAccessService, GameAccessService>();
builder.Services.AddScoped<ISaleService, SaleService>();
builder.Services.AddScoped<SeedService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddSingleton<IJsonFileStore, JsonFileStore>();
builder.Services.AddScoped<ISystemRequirementService, SystemRequirementService>();
builder.Services.AddScoped<IRefundService, RefundService>();
builder.Services.AddScoped<IGameVersionService, GameVersionService>();
builder.Services.AddScoped<ICreditService, CreditService>();
builder.Services.AddScoped<IFriendService, FriendService>();
builder.Services.AddScoped<IFriendSuggestionService, FriendSuggestionService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IPostService, PostService>();
builder.Services.AddScoped<ISupportTicketService, SupportTicketService>();
builder.Services.AddAutoMapper(typeof(MappingProfile)); // TECHNOLOGY: AutoMapper 12 - Entity → ViewModel mapping
builder.Services.AddSingleton<ConnectionTracker>();
var keysDir = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtectionKeys");
Directory.CreateDirectory(keysDir);
builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(keysDir)); // TECHNOLOGY: Data Protection - Persistent keys
builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("Stripe")); // TECHNOLOGY: Stripe - Payment processing
builder.Services.Configure<MailjetApiSettings>(builder.Configuration.GetSection("Mailjet")); // TECHNOLOGY: Mailjet - Email sending
builder.Services.AddScoped<IEmailService, MailjetEmailService>(); // TECHNOLOGY: Mailjet - Outbound email
builder.Services.AddHostedService<TokenCleanupService>(); // TECHNOLOGY: BG Service - Expired email token cleanup
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
builder.Services.Configure<BrotliCompressionProviderOptions>(o => o.Level = CompressionLevel.Optimal);
builder.Services.Configure<GzipCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);

// TECHNOLOGY: Output caching - reduces DB load on read-heavy endpoints
builder.Services.AddHealthChecks(); // TECHNOLOGY: Health checks - /health endpoint
builder.Services.AddOutputCache(o =>
{
    o.DefaultExpirationTimeSpan = TimeSpan.FromMinutes(10);
    o.AddPolicy("NoCache", b => b.NoCache());
});

// Session cache: Redis if available, otherwise in-memory
var redisConnection = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrEmpty(redisConnection))
{
    builder.Services.AddStackExchangeRedisCache(options => options.Configuration = redisConnection);
}
else
{
    builder.Services.AddDistributedMemoryCache();
}
builder.Services.AddSession(options => // TECHNOLOGY: Session Auth - Custom (no Identity)
{
    options.IdleTimeout        = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly    = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite    = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.Name        = ".GameStore.Session";
});

// Cookie-based authentication (replacing session-only auth)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath        = "/Auth/Login";
        options.LogoutPath       = "/Auth/Logout";
        options.AccessDeniedPath = "/Home/Index";
        options.Cookie.Name      = ".GameStore.Auth";
        options.Cookie.HttpOnly  = true;
        options.Cookie.SameSite  = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.ExpireTimeSpan   = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    })
    .AddGoogle(options =>
    {
        options.ClientId     = builder.Configuration["Google:ClientId"] ?? "";
        options.ClientSecret = builder.Configuration["Google:ClientSecret"] ?? "";
        options.CallbackPath = "/auth/google-callback";
        options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.SaveTokens   = true;
        options.Scope.Add("profile");
        options.Scope.Add("email");
    });

// TECHNOLOGY: Rate Limiting - Brute-force / abuse protection for auth endpoints
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("AuthLogin", ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    options.AddPolicy("AuthRegister", ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 3,
                Window = TimeSpan.FromMinutes(10),
                QueueLimit = 0
            }));

    options.AddPolicy("AuthResend", ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 3,
                Window = TimeSpan.FromMinutes(15),
                QueueLimit = 0
            }));

    options.AddPolicy("AuthVerify", ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly",     p => p.RequireClaim("Role", "ADMIN"));
    options.AddPolicy("DeveloperOnly", p => p.RequireClaim("Role", "DEVELOPER"));
    options.AddPolicy("CustomerOnly",  p => p.RequireClaim("Role", "CUSTOMER"));
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

    // Seed only if DB is empty (checks internally via AnyAsync)
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
            !path.StartsWithSegments("/lib") && !path.StartsWithSegments("/images") &&
            path != "/favicon.ico" && path != "/favicon.svg")
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

app.UseRateLimiter(); // TECHNOLOGY: Rate Limiting - Auth abuse protection
app.UseOutputCache(); // TECHNOLOGY: Output caching - 10 min default TTL
app.UseSession(); // TECHNOLOGY: Session - Auth state
app.UseAuthentication(); // TECHNOLOGY: Cookie Auth - ClaimsPrincipal
app.UseAuthorization(); // TECHNOLOGY: Auth - Role-based filters + policies

// TECHNOLOGY: Email verification gate - blocks unverified users from all pages except auth routes
app.Use(async (ctx, next) =>
{
    var userId = ctx.Session.GetString("UserId");
    var emailConfirmed = ctx.Session.GetString("EmailConfirmed");
    var path = ctx.Request.Path.Value?.ToLowerInvariant() ?? "";

    var allowed = new[]
    {
        "/auth/verifyemail", "/auth/verifyemailnotice",
        "/auth/login", "/auth/register", "/auth/logout",
        "/auth/forgotpassword", "/auth/resetpassword",
        "/auth/checkusername", "/auth/externallogin", "/auth/externalcallback",
        "/auth/completeregistration", "/home/error"
    };

    if (!string.IsNullOrEmpty(userId) && emailConfirmed == "False" &&
        !allowed.Any(a => path.StartsWith(a)) &&
        !path.StartsWith("/css") && !path.StartsWith("/js") &&
        !path.StartsWith("/lib") && !path.StartsWith("/images"))
    {
        ctx.Response.Redirect("/Auth/VerifyEmailNotice");
        return;
    }

    await next();
});

app.MapHealthChecks("/health"); // TECHNOLOGY: Health check - load balancer probe

app.MapHub<NotificationHub>("/hub/notifications"); // TECHNOLOGY: SignalR - Notification hub endpoint

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

