# GameStore — ASP.NET Core Razor Pages + MVC (.NET 8)

Steam-style game store with a full admin management portal.  
Built with **Razor Pages** (storefront + admin panel), **MVC** (utility pages), **Entity Framework Core**, and **session-based authentication**.

---

## Table of Contents

- [Quick Start](#quick-start)
- [Architecture](#architecture)
- [Project Structure](#project-structure)
- [Database Schema](#database-schema)
- [Authentication & Authorization](#authentication--authorization)
- [Pages & Routes](#pages--routes)
- [Services Layer](#services-layer)
- [Admin Panel](#admin-panel)
- [File Uploads](#file-uploads)
- [NuGet Packages](#nuget-packages)
- [Configuration](#configuration)

---

## Quick Start

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- SQL Server **or** SQL Server Express **or** LocalDB (LocalDB comes with Visual Studio)

### Setup

```bash
# 1. Navigate to the web project
cd GameStore_AdminComplete/GameStore.PL123/Game-Store

# 2. Configure your connection string in appsettings.json
#    (LocalDB is the default — works out of the box with Visual Studio)

# 3. Run
dotnet run
```

On first run, the app:
1. Connects to the database
2. Runs all EF Core migrations (creates all tables)
3. Seeds 12 game categories
4. Creates default **Admin** and **Developer** accounts

Open **https://localhost:5001** or **http://localhost:5000**

### Default Credentials

| Role      | Email                  | Password    |
|-----------|------------------------|-------------|
| **Admin**   | `admin@gamestore.com`  | `Admin@1234` |
| **Developer** | `dev@gamestore.com`    | `Dev@1234`  |

---

## Architecture

The project follows a **3-layer architecture**:

```
┌─────────────────────────────┐
│   GameStore.PL (Web App)    │  ← Presentation Layer
│   Razor Pages + MVC         │     (UI, routing, filters)
├─────────────────────────────┤
│   GameStore.BLL (Services)  │  ← Business Logic Layer
│   Services + Interfaces     │     (auth, CRUD, external APIs)
├─────────────────────────────┤
│   GameStore.DAL (Data)      │  ← Data Access Layer
│   EF Core DbContext + Entities│  (models, mappings, migrations)
└─────────────────────────────┘
```

The web app uses **both Razor Pages and MVC**:
- **Razor Pages** (`Pages/`) — storefront, authentication, admin panel
- **MVC** (`Controllers/` + `Views/`) — utility pages (Privacy, Error) and a `HomeController` that redirects to the Razor Pages storefront

### Middleware Pipeline (Program.cs)

```
UseExceptionHandler → UseHttpsRedirection → UseStaticFiles → UseRouting
→ UseSession → UseAuthorization → MapControllerRoute → MapRazorPages
```

Key points:
- **Session-based auth** with 8-hour idle timeout
- `AdminOnlyFilter` applied to all `/Admin/*` page routes
- Large upload support (up to 2 GB for game files)
- Mixed routing: MVC (`{controller}/{action}/{id}`) + Razor Pages (`/{page}`)

---

## Project Structure

```
GameStore.PL123/
├── Game-Store/                          ← Web Application
│   ├── Pages/
│   │   ├── Index.cshtml                 ← Public storefront (browse games)
│   │   ├── _ViewImports.cshtml          ← Shared usings + tag helpers
│   │   ├── _ViewStart.cshtml            ← Default layout config
│   │   ├── Auth/
│   │   │   ├── Login.cshtml(.cs)        ← Authentication
│   │   │   └── Logout.cshtml(.cs)       ← Session destruction
│   │   ├── Admin/
│   │   │   ├── Dashboard.cshtml(.cs)    ← Stats overview
│   │   │   ├── ManageGames.cshtml(.cs)  ← Full game CRUD
│   │   │   ├── ManageCategories.cshtml(.cs)  ← Categories CRUD
│   │   │   ├── ManageUsers.cshtml(.cs)  ← User/role management
│   │   │   ├── Orders.cshtml(.cs)       ← Order ledger + status updates
│   │   │   └── ManageReviews.cshtml(.cs) ← Review moderation
│   │   └── Shared/
│   │       └── _AdminLayout.cshtml      ← Admin panel layout (cyberpunk theme)
│   │
│   ├── Views/                           ← MVC views
│   │   ├── Home/
│   │   │   ├── Index.cshtml             ← (unused — redirects to store)
│   │   │   └── Privacy.cshtml
│   │   ├── Shared/
│   │   │   ├── _Layout.cshtml           ← Main store layout
│   │   │   └── Error.cshtml
│   │   └── _ViewImports.cshtml
│   │
│   ├── Controllers/
│   │   └── HomeController.cs            ← MVC: redirects to Razor Pages store
│   │
│   ├── Models/
│   │   └── ErrorViewModel.cs
│   │
│   ├── AdminOnlyFilter.cs               ← Auth filter for /Admin routes
│   ├── Program.cs                       ← App entry point + DI + pipeline
│   ├── appsettings.json                 ← Connection strings
│   └── wwwroot/
│       ├── css/, js/, lib/              ← Static assets
│       └── uploads/games/               ← Uploaded cover images, screenshots, files
│
├── GameStore.BLL/                       ← Business Logic Layer
│   └── Services/
│       ├── AuthService.cs               ← Login, register, password hashing
│       ├── GameService.cs               ← Game CRUD operations
│       ├── UserService.cs               ← User management + role changes
│       ├── CategoryService.cs           ← Category CRUD
│       ├── OrderService.cs              ← Order management + revenue stats
│       └── ReviewService.cs             ← Review moderation
│
└── GameStore.DAL/                       ← Data Access Layer
    ├── DataBase/
    │   └── GameStoreDbContext.cs         ← EF Core context + Fluent API config
    ├── Entities/
    │   ├── User.cs                      ← Users (ADMIN, DEVELOPER, CUSTOMER)
    │   ├── Game.cs                      ← Games with metadata + file info
    │   ├── Category.cs                  ← Game categories
    │   ├── GameCategory.cs              ← Many-to-many join
    │   ├── Cart.cs / CartItem.cs        ← Shopping cart
    │   ├── Order.cs / OrderItem.cs      ← Orders with line items
    │   ├── Library.cs / LibraryGame.cs  ← User game libraries
    │   └── Review.cs                    ← User reviews
    ├── Enum/
    │   ├── Role.cs                      ← ADMIN=0, CUSTOMER=1, DEVELOPER=2
    │   └── OrderStatus.cs               ← PENDING, COMPLETED, CANCELLED
    └── Migrations/                      ← EF Core migrations
```

---

## Database Schema

```
┌─────────┐       ┌──────────────┐       ┌────────────┐
│  Users  │──1:1──│    Carts     │──1:N──│  CartItems │──N:1──┐
│         │──1:1──│  Libraries   │──1:N──│LibraryGames│──N:1──┤
│         │──1:N──│   Orders     │──1:N──│ OrderItems  │──N:1──┤
│         │──1:N──│  Reviews     │──N:1──────────────────────┘
└─────────┘                              │   Games    │
                                         │            │
                                    ┌────│            │
                                    │    └────────────┘
                                    │         │N:N
                                    │    ┌──────────────┐
                                    └────│ GameCategories│──N:1──┐
                                         └──────────────┘       │
                                                            ┌──────────┐
                                                            │Categories│
                                                            └──────────┘
```

### Key Constraints

| Entity | Constraint |
|--------|-----------|
| `Users.Email` | UNIQUE |
| `Categories.Name` | UNIQUE |
| `Carts.UserId` | UNIQUE (one cart per user) |
| `Libraries.UserId` | UNIQUE (one library per user) |
| `CartItems(CartId, GameId)` | UNIQUE (no duplicate games in cart) |
| `Reviews(UserId, GameId)` | UNIQUE (one review per user per game) |
| `OrderItems.GameId` | RESTRICT delete (preserve order history) |
| `LibraryGames.GameId` | RESTRICT delete (preserve library history) |

### Game Entity — Special Properties

- **`ScreenshotUrls`** — stored as a JSON array string in `nvarchar(max)`, serialized/deserialized by EF Core value converter
- **`Price`** — `decimal(10,2)`, 0 = free
- **`GameFileUrl`** — relative path to the downloadable game file
- **`GameFileSizeBytes`** — raw bytes for display formatting

---

## Authentication & Authorization

### Session-Based Auth

No Identity Framework. Everything is built on raw **ASP.NET Core Session**:

```csharp
// On login:
HttpContext.Session.SetString("UserId", user.Id);
HttpContext.Session.SetString("Username", user.Username);
HttpContext.Session.SetString("Role", user.Role.ToString());

// On logout:
HttpContext.Session.Clear();
```

Session cookie: `.GameStore.Session` | Idle timeout: **8 hours** | HttpOnly: true

### Login Flow (Pages/Auth/Login.cshtml.cs)

1. GET — if already logged in as ADMIN, redirect to `/Admin/Dashboard`
2. POST — validate email/password via `AuthService.LoginAsync()`
3. Check role — only `ADMIN` and `DEVELOPER` are allowed in this build
4. Set session variables
5. ADMIN → redirect to `/Admin/Dashboard` | DEVELOPER → redirect to store

### Password Hashing

Passwords are hashed with **BCrypt** (`BCrypt.Net-Next`).  
Legacy SHA256 hashes are auto-upgraded to BCrypt on first successful login.

### AdminOnlyFilter (Pages/Admin/ route protection)

Registered globally for the `/Admin` folder convention in `Program.cs`:

```csharp
options.Conventions.AddFolderApplicationModelConvention("/Admin", model =>
{
    model.Filters.Add(new AdminOnlyFilter());
});
```

The filter checks `Session.GetString("Role") == "ADMIN"`.  
Non-admin users are redirected to `/Auth/Login`.

### Navbar Visibility (Views/Shared/_Layout.cshtml)

- **Logged out** → shows **Login** button
- **Logged in** → shows **username** + **Logout** button
- **ADMIN role** → also shows the **Admin** link
- DEVELOPER and CUSTOMER roles never see the Admin link

---

## Pages & Routes

### Public

| URL | Page | Description |
|-----|------|-------------|
| `/` | `Pages/Index.cshtml` | Storefront — browse games, search, filter by category, sort by price/date/name. Click to open detail modal |
| `/Index` | `Pages/Index.cshtml` | Same as `/` |
| `/Auth/Login` | `Pages/Auth/Login.cshtml` | Sign in with email + password |
| `/Auth/Logout` | `Pages/Auth/Logout.cshtml` | Clear session, redirect to login |

### Admin (ADMIN role required)

| URL | Page | Description |
|-----|------|-------------|
| `/Admin/Dashboard` | `Pages/Admin/Dashboard.cshtml` | Stats cards: total users, games, orders, revenue |
| `/Admin/ManageGames` | `Pages/Admin/ManageGames.cshtml` | Table of all games. Add/edit/delete with modal forms. Search filter |
| `/Admin/ManageCategories` | `Pages/Admin/ManageCategories.cshtml` | Category list with game counts. Add/edit/delete |
| `/Admin/ManageUsers` | `Pages/Admin/ManageUsers.cshtml` | User table. Change roles, delete users (prevent self-deletion) |
| `/Admin/Orders` | `Pages/Admin/Orders.cshtml` | Order history with status badges. Update PENDING/COMPLETED/CANCELLED |
| `/Admin/ManageReviews` | `Pages/Admin/ManageReviews.cshtml` | Review list. Delete inappropriate reviews |

### Utility (MVC)

| URL | Controller | Description |
|-----|-----------|-------------|
| `/Home/Privacy` | `HomeController.Privacy()` | Privacy policy page |
| `/Home/Error` | `HomeController.Error()` | Error page with request ID |

---

## Services Layer

### AuthService

| Method | Description |
|--------|-------------|
| `HashPassword(password)` | Static — BCrypt hash |
| `LoginAsync(email, password)` | Verify credentials, auto-upgrade legacy hashes |
| `RegisterAsync(username, email, password, role)` | Register new user |

### GameService

| Method | Description |
|--------|-------------|
| `GetAllWithCategoriesAsync()` | All games with categories, ordered by release date (read-only, `AsNoTracking`) |
| `GetByIdAsync(id)` | Single game with categories |
| `CreateAsync(game, categoryIds)` | Create game + assign categories |
| `UpdateAsync(id, update, categoryIds)` | Replace fields + re-assign categories |
| `DeleteAsync(id)` | Remove game |
| `GetTotalGamesAsync()` | Count for dashboard |

### UserService

| Method | Description |
|--------|-------------|
| `GetAllAsync()` | All users ordered by role then username |
| `GetByIdAsync(id)` | Single user |
| `ChangeRoleAsync(id, newRole, currentUserId)` | Prevent self-role-change |
| `DeleteAsync(id, currentUserId)` | Prevent self-deletion |
| `GetTotalUsersAsync()` | Count for dashboard |

### CategoryService

| Method | Description |
|--------|-------------|
| `GetAllAsync()` | All categories ordered by name |
| `GetAllWithGameCountAsync()` | Categories with game count |
| `CreateAsync(name)` | Create with duplicate name check |
| `UpdateAsync(id, name)` | Rename category |
| `DeleteAsync(id)` | Delete only if no games attached |

### OrderService

| Method | Description |
|--------|-------------|
| `GetAllWithDetailsAsync()` | All orders with user + items + games |
| `UpdateStatusAsync(orderId, status)` | Update PENDING/COMPLETED/CANCELLED |
| `GetTotalOrdersAsync()` | Total count |
| `GetCompletedOrdersAsync()` | Completed count |
| `GetTotalRevenueAsync()` | Sum of completed order totals |

### ReviewService

| Method | Description |
|--------|-------------|
| `GetAllWithDetailsAsync()` | All reviews with user + game details |
| `DeleteAsync(id)` | Remove review |

---

## Admin Panel

The admin panel uses a dedicated layout (`_AdminLayout.cshtml`) with a **cyberpunk** visual theme featuring:
- Dark color scheme with red/gold accents
- Custom fonts (Orbitron, Rajdhani, Share Tech Mono)
- CRT scan-line overlay
- Glow effects and glitch animations
- Ripple click effects on all interactive elements

### Manage Games — Edit Modal

The "Edit Game" modal uses a **pre-serialized JSON dataset** (`GameDataJson`) to avoid inline serialization in the Razor loop — improving page load performance with many games. The client-side search uses a **150ms debounce** to prevent layout thrashing on every keystroke.

### Dashboard Stats

- **Total Users** — `UserService.GetTotalUsersAsync()`
- **Total Games** — `GameService.GetTotalGamesAsync()`
- **Total Orders** — `OrderService.GetTotalOrdersAsync()`
- **Total Revenue** — `OrderService.GetTotalRevenueAsync()` (sum of COMPLETED orders)

---

---

## File Uploads

### Storage Layout

```
wwwroot/uploads/games/{gameId}/
    images/         ← Cover images + screenshots
    files/          ← Executables / archives
```

### Limits & Formats

| Setting | Value |
|---------|-------|
| Max file size | 2 GB |
| Accepted game formats | `.exe`, `.zip`, `.rar`, `.7z`, `.msi`, `.apk` |
| Accepted image formats | `.jpg`, `.jpeg`, `.png`, `.webp` |

Configured in `Program.cs`:
```csharp
builder.Services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = 2L * 1024 * 1024 * 1024);
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = 2L * 1024 * 1024 * 1024);
```

---

## NuGet Packages

### GameStore.PL (Web App)

| Package | Version | Purpose |
|---------|---------|---------|
| `Microsoft.EntityFrameworkCore.Design` | 9.0.8 | EF Core migrations tooling (design-time only) |

### GameStore.BLL (Business Logic)

| Package | Version | Purpose |
|---------|---------|---------|
| `Microsoft.Extensions.Configuration.Abstractions` | 9.0.8 | Access `appsettings.json` in services |
| `Microsoft.Extensions.Http` | 9.0.0 | `IHttpClientFactory` registration |

### GameStore.DAL (Data Access)

| Package | Version | Purpose |
|---------|---------|---------|
| `BCrypt.Net-Next` | 4.0.3 | Password hashing |
| `Microsoft.EntityFrameworkCore` | 9.0.8 | ORM |
| `Microsoft.EntityFrameworkCore.SqlServer` | 9.0.8 | SQL Server provider |
| `Microsoft.EntityFrameworkCore.Proxies` | 9.0.8 | Lazy loading proxies |
| `Microsoft.EntityFrameworkCore.Tools` | 9.0.8 | Migrations CLI (design-time only) |
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | 8.0.0 | Identity store (unused — included for potential migration) |

---

## Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "GameStoreConnection": "Server=(localdb)\\mssqllocaldb;Database=GameStoreDB;Trusted_Connection=True;"
  },
  "AllowedHosts": "*"
}
```

### Environment-Specific

`appsettings.Development.json` can override settings for development (e.g., different connection strings, relaxed HTTPS requirements).

---

## Performance Notes

- **Read queries use `AsNoTracking()`** for display-only data (games list, categories)
- **Game JSON data is serialized once server-side**, not per-row in the Razor loop
- **Client-side search is debounced** at 150ms to avoid DOM thrashing
- Session state uses `IDistributedMemoryCache` (in-memory, single-server only)
