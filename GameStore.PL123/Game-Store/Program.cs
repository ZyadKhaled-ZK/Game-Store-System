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

builder.Services.AddHttpClient<ClaudeService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<IGameService, GameService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IReviewService, ReviewService>();

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

    // Seed categories
    if (!db.Categories.Any())
    {
        var categories = new[]
        {
            "Action", "Adventure", "RPG", "Strategy", "Simulation",
            "Sports", "Racing", "Indie", "Puzzle", "Horror",
            "Shooter", "Fighting"
        };
        db.Categories.AddRange(categories.Select(name => new Category { Name = name }));
        db.SaveChanges();
    }

    // Seed default Developer account
    if (!db.Users.Any(u => u.Role == GameStore.DAL.Enum.Role.DEVELOPER))
    {
        db.Users.Add(new User
        {
            Username     = "Developer",
            Email        = "dev@gamestore.com",
            PasswordHash = AuthService.HashPassword("Dev@1234"),
            Role         = GameStore.DAL.Enum.Role.DEVELOPER
        });
        db.SaveChanges();
    }

    // Seed default Admin account
    if (!db.Users.Any(u => u.Role == GameStore.DAL.Enum.Role.ADMIN))
    {
        db.Users.Add(new User
        {
            Username     = "Admin",
            Email        = "admin@gamestore.com",
            PasswordHash = AuthService.HashPassword("Admin@1234"),
            Role         = GameStore.DAL.Enum.Role.ADMIN
        });
        db.SaveChanges();
    }
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
