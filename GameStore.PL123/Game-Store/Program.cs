using GameStore.PL;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ── Allow large uploads (up to 2 GB) ───────────────────────────────────────
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 2L * 1024 * 1024 * 1024;
});
builder.WebHost.ConfigureKestrel(o =>
{
    o.Limits.MaxRequestBodySize = 2L * 1024 * 1024 * 1024;
});

// ── Services ────────────────────────────────────────────────────────────────
builder.Services.AddControllersWithViews();

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AddFolderApplicationModelConvention("/Admin", model =>
    {
        model.Filters.Add(new AdminOnlyFilter());
    });
});

builder.Services.AddDbContext<GameStoreDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("GameStoreConnection")));

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
builder.Services.AddScoped<SeedService>();

// Session-based auth (8-hour idle timeout)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout        = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly    = true;
    options.Cookie.IsEssential = true;
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
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();
