using GameStore.PL.Hubs;
using GameStore.PL.Mappings;
using GameStore.PL.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// ── Upload limits (100 MB max) ─────────────────────────────────────────────
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 104_857_600;
});
builder.WebHost.ConfigureKestrel(o =>
{
    o.Limits.MaxRequestBodySize = 104_857_600;
});

// ── Services ────────────────────────────────────────────────────────────────
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

builder.Services.AddScoped<AdminOnlyFilter>();
builder.Services.AddScoped<DeveloperOnlyFilter>();

builder.Services.AddDbContext<GameStoreDbContext>(options =>
    options.UseSqlServer(
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
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IPostService, PostService>();
builder.Services.AddScoped<ISupportTicketService, SupportTicketService>();
builder.Services.AddAutoMapper(typeof(MappingProfile));
builder.Services.AddSingleton<ConnectionTracker>();
builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("Stripe"));
var stripeSecretKey = builder.Configuration["Stripe:SecretKey"];
if (!string.IsNullOrEmpty(stripeSecretKey))
    Stripe.StripeConfiguration.ApiKey = stripeSecretKey;
builder.Services.AddSignalR();
builder.Services.AddHttpContextAccessor();
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins()  // same-origin only by default
      .AllowCredentials()));

// Session-based auth (8-hour idle timeout)
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});
builder.Services.AddSession(options =>
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

app.UseHttpsRedirection();
app.UseStaticFiles();
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
app.UseCors();
app.UseSession();
app.UseAuthorization();

app.MapHub<NotificationHub>("/hub/notifications");

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

