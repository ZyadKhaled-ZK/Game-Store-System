# GameStore — ASP.NET Core 8 MVC Game Store

Steam-style digital game store with role-based access (Customer, Developer, Admin), real-time notifications, friend system, support tickets, sale/discount system, and full admin/developer management portals.

---

## Table of Contents

- [Quick Start](#quick-start)
- [Architecture](#architecture)
- [Project Structure](#project-structure)
- [Database Schema](#database-schema)
- [Authentication & Authorization](#authentication--authorization)
- [Controllers & Routes](#controllers--routes)
- [Services Layer](#services-layer)
- [Admin Panel](#admin-panel)
- [Developer Portal](#developer-portal)
- [Real-Time Features](#real-time-features)
- [AutoMapper](#automapper)
- [Redis Caching](#redis-caching)
- [File Uploads](#file-uploads)
- [NuGet Packages](#nuget-packages)
- [Configuration](#configuration)
- [Testing](#testing)
- [Performance Notes](#performance-notes)

---

## Quick Start

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- SQL Server, SQL Server Express, or LocalDB
- Redis (optional — session fallback works without it; see [Redis Caching](#redis-caching))
- Stripe account for payment processing (optional for development)

### Setup

```bash
# 1. Navigate to the web project
cd GameStore_AdminComplete/GameStore.PL123/Game-Store

# 2. Configure connection string in appsettings.json
#    LocalDB is the default — works out of the box with Visual Studio

# 3. Configure Stripe keys (User Secrets for Development):
dotnet user-secrets set "Stripe:SecretKey" "sk_test_..."
dotnet user-secrets set "Stripe:PublishableKey" "pk_test_..."
dotnet user-secrets set "Stripe:WebhookSecret" "whsec_..."

# 4. Run
dotnet run
```

On first run, the app:
1. Connects to the database
2. Runs all EF Core migrations (creates all tables)
3. Seeds 12 game categories (Action, Adventure, RPG, Strategy, etc.)
4. Creates default **Admin** and **Developer** accounts

Open **https://localhost:5001** or **http://localhost:5000**

### Default Credentials

| Role        | Email                  | Password     |
|-------------|------------------------|--------------|
| **Admin**   | `admin@gamestore.com`  | `Admin@1234` |
| **Developer** | `dev@gamestore.com`  | `Dev@1234`   |

---

## Architecture

3-layer architecture:

```
┌─────────────────────────────────┐
│  GameStore.PL (Web App)         │  ← Presentation Layer
│  MVC Controllers + Areas        │     (routing, views, SignalR, filters)
├─────────────────────────────────┤
│  GameStore.BLL (Services)       │  ← Business Logic Layer
│  20 service interfaces + impls  │     (auth, CRUD, analytics, notifications)
├─────────────────────────────────┤
│  GameStore.DAL (Data Access)    │  ← Data Access Layer
│  EF Core + Repository/UoW      │     (22 entities, 17 migrations, 4 enums)
└─────────────────────────────────┘
```

### Key Design Patterns

- **Repository + Unit of Work** — `IRepository<T>`, `IUnitOfWork` with `EfRepository<T>` and `UnitOfWork` implementations
- **Service Layer** — all business logic behind interfaces, injected via DI
- **Session-based auth** — no ASP.NET Identity; custom session management with 8-hour idle timeout
- **Filter attributes** — `AdminOnlyFilter` and `DeveloperOnlyFilter` for area protection

### Middleware Pipeline (Program.cs)

```
UseExceptionHandler → UseHttpsRedirection → UseStaticFiles
→ UseRouting → UseCors → UseSession → UseAuthorization
→ MapHub("/hub/notifications") → MapControllerRoute (areas) → MapControllerRoute (default)
```

---

## Project Structure

```
GameStore.PL123/
├── Game-Store/                                   ← ASP.NET Core 8 Web App
│   ├── Program.cs                                ← Entry point, DI, middleware, seed
│   ├── GlobalUsings.cs                           ← Global using directives (AutoMapper, BLL, DAL)
│   ├── appsettings.json                          ← Connection strings, Stripe, Redis config
│   ├── appsettings.Development.json              ← Dev overrides
│   │
│   ├── Controllers/                              ← 16 MVC controllers
│   │   ├── AuthController.cs                     ← Login, register, password reset, email update
│   │   ├── BecomeDeveloperController.cs          ← Developer application submission
│   │   ├── CartController.cs                     ← Cart view, add/remove, Stripe checkout
│   │   ├── ChatController.cs                     ← Real-time messaging
│   │   ├── DevelopersController.cs               ← Developer studio profiles
│   │   ├── FriendsController.cs                  ← Friend requests, suggestions, search
│   │   ├── HomeController.cs                     ← Home page, privacy, error
│   │   ├── LibraryController.cs                  ← User game library, game download
│   │   ├── NotificationsController.cs            ← Notification dropdown API
│   │   ├── OrdersController.cs                   ← Order history
│   │   ├── PostsController.cs                    ← Profile posts (create, delete, spam cooldown)
│   │   ├── ProfileController.cs                  ← User profile view/edit
│   │   ├── ReviewsController.cs                  ← Review submission (JSON endpoint)
│   │   ├── StripeWebhookController.cs            ← Stripe webhook handler
│   │   ├── SupportController.cs                  ← Support ticket creation, my tickets, replies
│   │   └── WishlistController.cs                 ← Wishlist view, toggle, add to cart
│   │
│   ├── Areas/                                    ← Area-based modules
│   │   ├── Admin/                                ← Admin management portal
│   │   │   ├── Controllers/
│   │   │   │   ├── CategoriesController.cs       ← Category CRUD
│   │   │   │   ├── DashboardController.cs        ← Analytics dashboard
│   │   │   │   ├── DeveloperApplicationsController.cs  ← Approve/reject developer apps
│   │   │   │   ├── DevelopersController.cs       ← Manage developer studios
│   │   │   │   ├── GamesController.cs            ← Game CRUD
│   │   │   │   ├── OrdersController.cs           ← Order management
│   │   │   │   ├── ReviewsController.cs          ← Review moderation
│   │   │   │   ├── SalesController.cs            ← Approve/reject sale requests
│   │   │   │   ├── SupportTicketsController.cs   ← Ticket management, replies, status
│   │   │   │   └── UsersController.cs            ← User management, role changes
│   │   │   └── Views/
│   │   │       ├── Shared/_AdminLayout.cshtml    ← Cyberpunk-themed admin layout
│   │   │       ├── Categories/Index.cshtml
│   │   │       ├── Dashboard/Index.cshtml
│   │   │       ├── DeveloperApplications/Index.cshtml
│   │   │       ├── Developers/{Index,Details,Edit}.cshtml
│   │   │       ├── Games/Index.cshtml
│   │   │       ├── Orders/Index.cshtml
│   │   │       ├── Reviews/Index.cshtml
│   │   │       ├── Sales/Index.cshtml
│   │   │       ├── SupportTickets/{Index,Details}.cshtml
│   │   │       └── Users/{Index,Details}.cshtml
│   │   │
│   │   └── Developer/                            ← Developer portal
│   │       ├── Controllers/
│   │       │   ├── DashboardController.cs        ← Developer stats (downloads, revenue, reviews)
│   │       │   ├── GamesController.cs            ← Game management for developer
│   │       │   ├── ProfileController.cs          ← Developer studio profile
│   │       │   ├── ReviewsController.cs          ← View game reviews
│   │       │   └── SalesController.cs            ← Create sale requests
│   │       └── Views/
│   │           ├── Shared/_DeveloperLayout.cshtml
│   │           ├── Dashboard/Index.cshtml
│   │           ├── Games/Index.cshtml
│   │           ├── Profile/Index.cshtml
│   │           ├── Reviews/Index.cshtml
│   │           └── Sales/{Index,Create}.cshtml
│   │
│   ├── Filters/                                  ← Authorization filters
│   │   ├── AdminOnlyFilter.cs                    ← Restricts Admin area to ADMIN role
│   │   └── DeveloperOnlyFilter.cs                ← Restricts Developer area to DEVELOPER role
│   │
│   ├── Hubs/                                     ← SignalR real-time hubs
│   │   ├── NotificationHub.cs                    ← User notifications, connection management
│   │   └── ConnectionTracker.cs                  ← Tracks online users, last seen
│   │
│   ├── Mappings/
│   │   └── MappingProfile.cs                     ← AutoMapper profiles (Post→PostViewModel, Review→ReviewDto)
│   │
│   ├── Services/                                 ← Presentation-layer services
│   │   ├── INotificationService.cs               ← Send notifications to users/admins via SignalR
│   │   ├── NotificationService.cs                ← Implementation (persists + broadcasts)
│   │   └── StripeSettings.cs                     ← Stripe configuration model
│   │
│   ├── Models/                                   ← ViewModels
│   │   ├── Admin/  (8 files)                     ← Dashboard, ManageGames, ManageUsers, etc.
│   │   ├── Auth/   (6 files)                     ← Login, Register, ResetPassword, etc.
│   │   ├── Cart/   (CartViewModel.cs)
│   │   ├── Chat/   (ChatIndexViewModel.cs)
│   │   ├── Friends/ (FriendsIndexViewModel.cs)
│   │   ├── Home/   (HomeViewModel.cs, ReviewRequest.cs)
│   │   ├── Library/ (LibraryViewModel.cs)
│   │   ├── Orders/ (OrderDetailViewModel.cs, OrderListViewModel.cs)
│   │   ├── Wishlist/ (WishlistViewModel.cs)
│   │   └── ErrorViewModel.cs
│   │
│   ├── Views/                                    ← 30+ MVC views
│   │   ├── Shared/_Layout.cshtml                 ← Main store layout (cyberpunk theme)
│   │   ├── Home/{Index,Privacy}.cshtml
│   │   ├── Auth/{Login,Register,ForgotPassword,ResetPassword}.cshtml
│   │   ├── Cart/{Index,Success}.cshtml
│   │   ├── Chat/Index.cshtml
│   │   ├── Developers/Index.cshtml
│   │   ├── Friends/{Index,Requests}.cshtml
│   │   ├── Library/Index.cshtml
│   │   ├── Orders/{Index,Details}.cshtml
│   │   ├── Profile/{Index,Edit}.cshtml
│   │   ├── Support/{Index,MyTickets,Details}.cshtml
│   │   └── Wishlist/Index.cshtml
│   │
│   └── wwwroot/                                  ← Static files
│       ├── css/site.css
│       ├── js/site.js
│       ├── lib/ (bootstrap, jQuery, validation)
│       └── uploads/ (avatars/, cvs/, games/{id}/{files,images}/)
│
├── GameStore.BLL/                                ← Business Logic Layer
│   ├── Services/                                 ← 20 interfaces + 20 implementations
│   │   ├── AuthService.cs / IAuthService.cs
│   │   ├── CartService.cs / ICartService.cs
│   │   ├── CategoryService.cs / ICategoryService.cs
│   │   ├── ChatService.cs / IChatService.cs
│   │   ├── DeveloperApplicationService.cs / IDeveloperApplicationService.cs
│   │   ├── DeveloperService.cs / IDeveloperService.cs
│   │   ├── FriendService.cs / IFriendService.cs
│   │   ├── FriendSuggestionService.cs / IFriendSuggestionService.cs
│   │   ├── GameFileService.cs / IGameFileService.cs
│   │   ├── GameService.cs / IGameService.cs
│   │   ├── LibraryService.cs / ILibraryService.cs
│   │   ├── OrderAnalyticsService.cs / IOrderAnalyticsService.cs
│   │   ├── OrderService.cs / IOrderService.cs
│   │   ├── PostService.cs / IPostService.cs
│   │   ├── ReviewService.cs / IReviewService.cs
│   │   ├── SaleService.cs / ISaleService.cs
│   │   ├── SeedService.cs (no interface)
│   │   ├── SupportTicketService.cs / ISupportTicketService.cs
│   │   ├── UserService.cs / IUserService.cs
│   │   └── WishlistService.cs / IWishlistService.cs
│   │
│   └── Models/
│       ├── GameExtensions.cs                     ← PagedResult<T>, GamesByCategory
│       └── ReportData.cs                         ← Analytics report models
│
├── GameStore.DAL/                                ← Data Access Layer
│   ├── DataBase/
│   │   └── GameStoreDbContext.cs                 ← EF Core context + Fluent API config (22 DbSets)
│   │
│   ├── Entities/                                 ← 22 entity classes
│   │   ├── User.cs, Game.cs, Category.cs
│   │   ├── Cart.cs, CartItem.cs
│   │   ├── Order.cs, OrderItem.cs
│   │   ├── Library.cs, LibraryGame.cs
│   │   ├── Review.cs, Post.cs
│   │   ├── WishlistItem.cs
│   │   ├── PasswordResetToken.cs
│   │   ├── Developer.cs, DeveloperApplication.cs
│   │   ├── Sale.cs
│   │   ├── Friendship.cs (+ FriendshipStatus enum)
│   │   ├── Message.cs
│   │   ├── SupportTicket.cs, SupportTicketReply.cs
│   │   ├── UserNotification.cs
│   │   └── GameCategory.cs
│   │
│   ├── Enum/                                     ← 4 enums
│   │   ├── Role.cs (ADMIN, CUSTOMER, DEVELOPER)
│   │   ├── SaleStatus.cs (Pending, Approved, Rejected, Cancelled, Expired)
│   │   ├── TicketStatus.cs (Open, InProgress, Resolved, Closed)
│   │   └── PaymentStatus.cs (Pending, Paid, Failed, Refunded)
│   │
│   ├── Repo/                                     ← Repository + Unit of Work
│   │   ├── IRepository.cs                        ← Generic CRUD interface
│   │   ├── IUnitOfWork.cs                        ← Transaction + SaveChanges
│   │   ├── EfRepository.cs                       ← EF Core implementation
│   │   └── UnitOfWork.cs                         ← UoW implementation
│   │
│   └── Migrations/                               ← 17 migration pairs + snapshot
│
└── GameStore.Tests/                              ← xUnit test project
    ├── Services/                                 ← 15 test files (614 tests)
    │   ├── AuthServiceTests.cs
    │   ├── CartServiceTests.cs
    │   ├── CategoryServiceTests.cs
    │   ├── ChatServiceTests.cs
    │   ├── DeveloperApplicationServiceTests.cs
    │   ├── DeveloperServiceTests.cs
    │   ├── FriendServiceTests.cs
    │   ├── FriendSuggestionServiceTests.cs        ← NEW — scoring algorithm
    │   ├── GameFileServiceTests.cs
    │   ├── GameServiceTests.cs
    │   ├── LibraryServiceTests.cs
    │   ├── OrderServiceTests.cs
    │   ├── PostServiceTests.cs                    ← NEW — post CRUD + spam cooldown
    │   ├── ReviewServiceTests.cs
    │   ├── SaleServiceTests.cs                    ← NEW — sale lifecycle
    │   ├── SupportTicketServiceTests.cs           ← NEW — ticket CRUD + replies
    │   ├── OrderAnalyticsServiceTests.cs          ← NEW — analytics queries
    │   ├── SeedServiceTests.cs                    ← NEW — seed idempotency
    │   ├── UserServiceTests.cs
    │   └── WishlistServiceTests.cs
    └── GlobalUsings.cs + csproj
```

---

## Database Schema

### Entities & Relationships

```
Users (1)──(1) Carts ────(1:N) CartItems ────(N:1)
Users (1)──(1) Libraries ──(1:N) LibraryGames ──(N:1)
Users (1)──(1:N) Orders ──(1:N) OrderItems ──(N:1)     Games
Users (1)──(1:N) Reviews ──(N:1)───────────────(N:1)    Games
Users (1)──(1:N) WishlistItems ──(N:1)──────────(N:1)   Games
Users (1)──(1:N) Posts
Users (1)──(1:N) SentFriendRequests ────(N:1)──────────── Friendship
Users (1)──(1:N) ReceivedFriendRequests ──(N:1)───────── Friendship
Users (1)──(1:N) SentMessages ────────(N:1)───────────── Message
Users (1)──(1:N) ReceivedMessages ────(N:1)───────────── Message
Users (1)──(1:N) SupportTickets ────(1:N) SupportTicketReplies
Users (1)──(1:1) Developer ────(1:N) Games
Users (1)──(1:N) DeveloperApplications

Games (N:N) Categories ← GameCategories (join)
Games (1)──(1:N) Sales (Developer → Sale → Game)
Sales  (N:1)── Developers
Games (1)──(1:N) GameCategories ──(N:1) Categories

UserNotifications: UserId (nullable for broadcast) + SenderUserId (nullable)
PasswordResetTokens: UserId (FK) + Token (unique)
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
| `WishlistItems(UserId, GameId)` | UNIQUE |
| `Friendships(RequesterId, ReceiverId)` | UNIQUE |
| `PasswordResetTokens.Token` | UNIQUE |
| `Orders.StripeSessionId` | UNIQUE (filtered, nullable) |
| `Developers.Slug` | UNIQUE (filtered, nullable) |
| `OrderItems.GameId` | RESTRICT delete (preserve order history) |
| `LibraryGames.GameId` | RESTRICT delete |
| `Friendships.*Id` | RESTRICT delete for Requester/Receiver |
| `Messages.*Id` | RESTRICT delete for Sender/Receiver |
| `SupportTickets.UserId` | SET NULL on user delete |
| `SupportTicketReplies.UserId` | SET NULL on user delete |
| `Games.DeveloperId` | SET NULL on developer delete |

### Game Entity — Special Properties

- **`ScreenshotUrls`** — stored as JSON array string in `nvarchar(max)`, serialized/deserialized by EF Core value converter
- **`Price`** — `decimal(10,2)`, 0 = free
- **`GameFileUrl`** — relative path to downloadable game file
- **`GameFileSizeBytes`** — raw bytes for display formatting

---

## Authentication & Authorization

### Session-Based Auth (No ASP.NET Identity)

```csharp
// On login:
HttpContext.Session.SetString("UserId", user.Id);
HttpContext.Session.SetString("Username", user.Username);
HttpContext.Session.SetString("Role", user.Role.ToString());

// On logout:
HttpContext.Session.Clear();
```

Session cookie: `.GameStore.Session` | Idle timeout: **8 hours** | HttpOnly: true | SameSite: Strict | Secure: Always

### Role System

| Role | Value | Access |
|------|-------|--------|
| `ADMIN` | 0 | All areas, admin panel |
| `CUSTOMER` | 1 | Store, profile, friends, chat, library, support |
| `DEVELOPER` | 2 | Store + developer portal (games, sales, reviews, stats) |

### Authorization Filters

- **`AdminOnlyFilter`** — registered on `/Admin` area; checks `Session.GetString("Role") == "ADMIN"`; redirects to login
- **`DeveloperOnlyFilter`** — registered on `/Developer` area; checks `Session.GetString("Role") == "DEVELOPER"`; redirects to login

### Password Hashing

Passwords hashed with **BCrypt** (`BCrypt.Net-Next` v4.0.3). Legacy SHA256 hashes auto-upgraded to BCrypt on first successful login.

---

## Controllers & Routes

### Public

| URL | Controller | Action | Description |
|-----|-----------|--------|-------------|
| `/` | HomeController | Index | Storefront — browse games, hero carousel, reviews, cart/wishlist state |
| `/Auth/Login` | AuthController | Login | Sign in |
| `/Auth/Register` | AuthController | Register | Create account |
| `/Auth/ForgotPassword` | AuthController | ForgotPassword | Password reset request |
| `/Auth/ResetPassword` | AuthController | ResetPassword | Reset with token |
| `/Auth/ChangePassword` | AuthController | ChangePassword | Change password (authenticated) |
| `/Auth/UpdateEmail` | AuthController | UpdateEmail | Change email (authenticated) |
| `/Auth/Logout` | AuthController | Logout | Clear session |
| `/Profile/{username}` | ProfileController | Index | View user profile with posts |
| `/Profile/Edit` | ProfileController | Edit | Edit avatar/bio |
| `/Developers/{slug}` | DevelopersController | Index | Developer studio page |
| `/Cart` | CartController | Index | Shopping cart with Stripe checkout |
| `/Wishlist` | WishlistController | Index | User wishlist |
| `/Library` | LibraryController | Index | User game library |
| `/Library/Download/{id}` | LibraryController | Download | Download owned game file |
| `/Orders` | OrdersController | Index | Order history |
| `/Orders/{id}` | OrdersController | Details | Order detail |
| `/Friends` | FriendsController | Index | Friends list, requests, suggestions, chat |
| `/Friends/Requests` | FriendsController | Requests | Pending friend requests |
| `/Chat` | ChatController | Index | Real-time messaging |
| `/Support` | SupportController | Index | Create support ticket |
| `/Support/MyTickets` | SupportController | MyTickets | User's ticket list |
| `/Support/Details/{id}` | SupportController | Details | Ticket detail with replies |
| `/Notifications` | NotificationsController | Index | Redirects to home (dropdown-only UI) |
| `/BecomeDeveloper` | BecomeDeveloperController | Index | Apply for developer status |
| `/Home/Privacy` | HomeController | Privacy | Privacy policy |
| `/Home/Error` | HomeController | Error | Error page |

### AJAX Endpoints

| URL | Controller | Action | Description |
|-----|-----------|--------|-------------|
| `POST /Cart/AddToCart` | CartController | AddToCart | Add game to cart (JSON) |
| `POST /Wishlist/ToggleWishlist` | WishlistController | ToggleWishlist | Toggle wishlist (JSON) |
| `POST /Reviews/AddReview` | ReviewsController | AddReview | Submit review (JSON) |
| `POST /Posts/Create` | PostsController | Create | Create profile post (AJAX) |
| `POST /Posts/Delete` | PostsController | Delete | Delete profile post (AJAX) |
| `POST /Support/Reply` | SupportController | Reply | Reply to ticket (AJAX) |
| `POST /Cart/GetCount` | CartController | GetCount | Cart item count (JSON) |
| `POST /Auth/ValidateEmail` | AuthController | ValidateEmail | Check email availability |
| `GET /Friends/Search` | FriendsController | Search | Search users (JSON) |
| `GET /Friends/GetNotificationData` | FriendsController | GetNotificationData | Pending requests + unread |
| `POST /Friends/SendRequest` | FriendsController | SendRequest | Send friend request (JSON) |
| `POST /Friends/AcceptRequest` | FriendsController | AcceptRequest | Accept friend request (JSON) |
| `POST /Friends/RejectRequest` | FriendsController | RejectRequest | Reject friend request (JSON) |
| `POST /Friends/RemoveFriend` | FriendsController | RemoveFriend | Remove friend (JSON) |
| `POST /StripeWebhook` | StripeWebhookController | Index | Stripe payment webhook |

### Admin Area (`/Admin`)

| URL | Controller | Description |
|-----|-----------|-------------|
| `/Admin/Dashboard` | DashboardController | Stats: users, games, orders, revenue, charts |
| `/Admin/Games` | GamesController | Full game CRUD with search |
| `/Admin/Categories` | CategoriesController | Category management with game counts |
| `/Admin/Users` | UsersController | User list, role changes, delete |
| `/Admin/Reviews` | ReviewsController | Review moderation (delete inappropriate) |
| `/Admin/Orders` | OrdersController | Order history and management |
| `/Admin/Developers` | DevelopersController | Developer studio management |
| `/Admin/DeveloperApplications` | DeveloperApplicationsController | Approve/reject developer apps |
| `/Admin/Sales` | SalesController | Approve/reject sale requests |
| `/Admin/SupportTickets` | SupportTicketsController | Ticket management (reply, status change, delete) |

### Developer Area (`/Developer`)

| URL | Controller | Description |
|-----|-----------|-------------|
| `/Developer/Dashboard` | DashboardController | Game stats: downloads, revenue, reviews, avg rating |
| `/Developer/Games` | GamesController | Manage own games (CRUD) |
| `/Developer/Profile` | ProfileController | Studio profile (name, slug, description, logo, etc.) |
| `/Developer/Reviews` | ReviewsController | View reviews for own games |
| `/Developer/Sales` | SalesController | Create sale requests, view history |

---

## Services Layer

### IAuthService — Authentication

| Method | Description |
|--------|-------------|
| `LoginAsync(email, password)` | Verify credentials, auto-upgrade legacy hashes |
| `RegisterAsync(username, email, password, role)` | Register new user |
| `ChangePasswordAsync(userId, currentPassword, newPassword)` | Change password |
| `GenerateResetTokenAsync(email)` | Generate password reset token |
| `ResetPasswordAsync(token, newPassword)` | Reset password with token |
| `UpdateEmailAsync(userId, newEmail)` | Change email address |

### IGameService — Game Catalog

| Method | Description |
|--------|-------------|
| `GetAllWithCategoriesAsync()` | All games with categories (AsNoTracking) |
| `GetPagedAsync(page, pageSize)` | Paginated game list |
| `GetByIdAsync(id)` | Single game with categories |
| `CreateAsync(game, categoryIds)` | Create + assign categories |
| `UpdateAsync(id, update, categoryIds)` | Update + reassign categories |
| `DeleteAsync(id)` | Delete game |
| `GetTotalGamesAsync()` | Count for dashboard |
| `GetFreeGamesCountAsync()` | Count of free games |
| `GetGamesByCategoryAsync()` | Games grouped by category |

### IUserService — User Management

| Method | Description |
|--------|-------------|
| `GetAllAsync()` | All users ordered by role then username |
| `GetByIdAsync(id)` | Single user |
| `ChangeRoleAsync(id, newRole, currentUserId)` | Change role (prevent self-change) |
| `DeleteAsync(id, currentUserId)` | Delete user (prevent self-delete) |
| `GetTotalUsersAsync()` | Count for dashboard |
| `GetUsersByRoleAsync()` | Users grouped by role |
| `GetUsersByMonthAsync(months)` | Registrations per month |
| `GetUserByUsernameAsync(username)` | Lookup by username |
| `SearchUsersAsync(query)` | Search by username/email |
| `UpdateProfileAsync(userId, avatarUrl, bio)` | Update profile |
| `GetUsersByIdsAsync(ids)` | Batch lookup by IDs |

### ICategoryService — Categories

| Method | Description |
|--------|-------------|
| `GetAllAsync()` | All categories ordered by name |
| `GetAllWithGameCountAsync()` | Categories with game count |
| `CreateAsync(name)` | Create with duplicate check |
| `UpdateAsync(id, name)` | Rename |
| `DeleteAsync(id)` | Delete only if no games attached |

### ICartService — Shopping Cart

| Method | Description |
|--------|-------------|
| `GetCartItemsAsync(userId)` | Cart items with game details |
| `GetCartCountAsync(userId)` | Item count |
| `AddToCartAsync(userId, gameId)` | Add game (checks duplicates) |
| `RemoveFromCartAsync(cartItemId, userId)` | Remove item |
| `ClearCartAsync(userId)` | Empty cart |

### IOrderService — Orders

| Method | Description |
|--------|-------------|
| `GetAllWithDetailsAsync()` | All orders with user + items + games |
| `PlaceOrderAsync(userId)` | Create order (legacy) |
| `GetOrdersByUserAsync(userId)` | User's orders |
| `GetOrderByIdAsync(orderId)` | Single order with details |
| `CompleteCheckoutAsync(userId, stripeSessionId, stripePaymentIntentId)` | Paid checkout |
| `CompleteFreeCheckoutAsync(userId)` | Free checkout (no payment) |
| `GetByStripeSessionIdAsync(sessionId)` | Lookup by Stripe session |
| `GetRecentWithDetailsAsync(count)` | Recent orders |

### IOrderAnalyticsService — Revenue Analytics

| Method | Description |
|--------|-------------|
| `GetTotalOrdersAsync()` | Total order count |
| `GetTotalRevenueAsync()` | Sum of total prices |
| `GetAverageOrderValueAsync()` | Average order total |
| `GetRevenueByMonthAsync(months)` | Revenue grouped by year/month |
| `GetOrdersByDayAsync(days)` | Orders grouped by date |
| `GetTopSellingGamesAsync(count)` | Top N games by units sold |
| `GetRevenueByCategoryAsync()` | Revenue by category (join through GameCategories) |
| `GetOrdersCountByMonthAsync(months)` | Order count by month |
| `GetOrderCountSinceAsync(since)` | Count since date |
| `GetRevenueSinceAsync(since)` | Revenue since date |

### IWishlistService — Wishlist

| Method | Description |
|--------|-------------|
| `GetWishlistAsync(userId)` | Wishlist items with game details |
| `IsInWishlistAsync(userId, gameId)` | Check if in wishlist |
| `AddToWishlistAsync(userId, gameId)` | Add (checks duplicates) |
| `RemoveFromWishlistAsync(wishlistItemId, userId)` | Remove |
| `GetWishlistCountAsync(userId)` | Count |

### ILibraryService — Game Library

| Method | Description |
|--------|-------------|
| `GetLibraryGamesAsync(userId)` | Library games with game details |
| `HasGame(userId, gameId)` | Ownership check |
| `AddGameToLibraryAsync(userId, gameId)` | Add after purchase |

### IReviewService — Reviews

| Method | Description |
|--------|-------------|
| `GetAllWithDetailsAsync()` | All reviews with user + game |
| `DeleteAsync(id)` | Remove review |
| `GetByGameAsync(gameId)` | Reviews for a game |
| `GetByUserAsync(userId)` | Reviews by a user |
| `CreateAsync(userId, gameId, rating, comment)` | Create review (one per user per game) |

### IPostService — Profile Posts

| Method | Description |
|--------|-------------|
| `CreateAsync(userId, content)` | Create post |
| `GetUserPostsAsync(userId, page, pageSize)` | Paginated posts (ordered by CreatedAt desc) |
| `DeleteAsync(postId, userId)` | Delete own post (ownership check) |
| `GetUserPostCountAsync(userId)` | Count |
| `GetLastPostTimeAsync(userId)` | Last post timestamp (for spam cooldown — 30s) |

### IFriendService — Friends

| Method | Description |
|--------|-------------|
| `GetFriendsAsync(userId)` | Accepted friendships |
| `GetPendingRequestsAsync(userId)` | Pending requests for user |
| `GetFriendIdsAsync(userId)` | All friend user IDs |
| `SendRequestAsync(requesterId, receiverUsername)` | Send/auto-accept/re-send |
| `AcceptRequestAsync(friendshipId, userId)` | Accept (ownership check) |
| `RejectRequestAsync(friendshipId, userId)` | Reject (ownership check) |
| `RemoveFriendAsync(friendshipId, userId)` | Remove (ownership check) |

### IFriendSuggestionService — Friend Suggestions

| Method | Description |
|--------|-------------|
| `GetSuggestionsAsync(userId, count)` | Scoring algorithm based on mutual games, shared categories, same developers, wishlist overlap, mutual friends |

### IChatService — Messaging

| Method | Description |
|--------|-------------|
| `SendMessageAsync(senderId, receiverId, content)` | Send message |
| `GetConversationAsync(userId1, userId2, page, pageSize)` | Paginated conversation |
| `GetUnreadCountAsync(userId)` | Unread message count |
| `MarkAsReadAsync(senderId, receiverId)` | Mark conversation as read |
| `GetConversationsAsync(userId)` | All conversations with last message |

### IDeveloperService — Developer Studios

| Method | Description |
|--------|-------------|
| `GetByUserIdAsync(userId)` | Developer by user ID |
| `GetByIdAsync(id)` | Developer by ID |
| `GetBySlugAsync(slug)` | Developer by URL slug |
| `GetAllAsync()` | All developers |
| `GetGamesAsync(developerId)` | Developer's games |
| `CreateOrUpdateProfileAsync(...)` | Create or update studio profile |
| `GetDashboardStatsAsync(developerId)` | Game count, downloads, reviews, revenue, avg rating |
| `GetGameStatsAsync(developerId)` | Per-game stats |
| `IsDeveloperUserAsync(userId)` | Check if user has developer profile |
| `DeleteAsync(developerId)` | Delete studio |
| `DemoteAsync(developerId, currentUserId)` | Demote developer to customer |
| `ReactivateAsync(developerId, currentUserId)` | Reactivate developer |

### IDeveloperApplicationService — Developer Applications

| Method | Description |
|--------|-------------|
| `GetByIdAsync(id)` | Single application |
| `GetByUserIdAsync(userId)` | User's application |
| `GetAllAsync()` | All applications |
| `GetPendingAsync()` | Pending applications |
| `SubmitAsync(...)` | Submit application (with optional CV + GitHub) |
| `ApproveAsync(applicationId)` | Approve → creates Developer + role change |
| `RejectAsync(applicationId)` | Reject with status update |

### ISaleService — Sales/Discounts

| Method | Description |
|--------|-------------|
| `GetByIdAsync(id)` | Sale with Game + Developer |
| `GetPendingAsync()` | Pending sales for admin approval |
| `GetByDeveloperAsync(developerId)` | Developer's sales |
| `GetActiveSalesByGameIdsAsync(gameIds)` | Active approved sales (within date range) |
| `CreateAsync(developerId, gameId, newPrice, startDate, endDate)` | Create (validates price, dates, ownership; cancels previous pending) |
| `ApproveAsync(id)` | Admin approve |
| `RejectAsync(id, reason)` | Admin reject with reason |

### ISupportTicketService — Support Tickets

| Method | Description |
|--------|-------------|
| `CreateAsync(userId, email, subject, message)` | Create ticket (userId nullable for anonymous) |
| `GetByIdAsync(id)` | Ticket with User + Replies (ordered) |
| `GetUserTicketsAsync(userId, page, pageSize)` | Paginated user tickets |
| `GetUserTicketCountAsync(userId)` | User's ticket count |
| `GetAllAsync(page, pageSize)` | All tickets (admin) paginated |
| `GetCountAsync()` | Total count |
| `AddReplyAsync(ticketId, userId, message)` | Add reply (updates ticket UpdatedAt) |
| `UpdateStatusAsync(ticketId, status)` | Update status + UpdatedAt |
| `IsOwnerAsync(ticketId, userId)` | Ownership check |

### IGameFileService — Game File Management

| Method | Description |
|--------|-------------|
| `UpdateGameFileAsync(id, fileUrl, fileName, fileSize)` | Update game file metadata |
| `ClearGameFileAsync(id)` | Remove game file |
| `AddScreenshotAsync(gameId, url)` | Add screenshot |
| `RemoveScreenshotAsync(gameId, url)` | Remove screenshot |

### INotificationService (PL) — Real-Time Notifications

| Method | Description |
|--------|-------------|
| `SendToUserAsync(userId, title, message, type, category, referenceId, referenceUrl, senderUserId)` | Create notification + SignalR broadcast |
| `SendToAdminsAsync(title, message, type, category, referenceId, referenceUrl)` | Create broadcast notification (UserId=null) + SignalR broadcast to Admins group |

### SeedService — Data Seeding

| Method | Description |
|--------|-------------|
| `SeedAsync()` | Seeds categories (12), developer user, admin user — idempotent |

---

## Admin Panel

The admin panel (`/Admin` area) uses a dedicated **cyberpunk** layout with:
- Dark color scheme with red/gold accents
- Custom fonts (Orbitron, Rajdhani, Share Tech Mono)
- CRT scan-line overlay, glow effects, glitch animations
- Ripple click effects

### Dashboard Stats

| Metric | Source |
|--------|--------|
| Total Users | `UserService.GetTotalUsersAsync()` |
| Total Games | `GameService.GetTotalGamesAsync()` |
| Total Orders | `OrderAnalyticsService.GetTotalOrdersAsync()` |
| Total Revenue | `OrderAnalyticsService.GetTotalRevenueAsync()` |
| Revenue by Month | `OrderAnalyticsService.GetRevenueByMonthAsync()` |
| Orders by Day | `OrderAnalyticsService.GetOrdersByDayAsync()` |
| Top Selling Games | `OrderAnalyticsService.GetTopSellingGamesAsync()` |
| Revenue by Category | `OrderAnalyticsService.GetRevenueByCategoryAsync()` |
| Orders by Month | `OrderAnalyticsService.GetOrdersCountByMonthAsync()` |

### Manage Games

Edit modal uses pre-serialized JSON dataset (`GameDataJson`) to avoid inline serialization in Razor loop. Client-side search uses **150ms debounce**.

---

## Developer Portal

Developer portal (`/Developer` area) provides:
- **Dashboard** — game stats (downloads, revenue, review count, average rating per game)
- **Games** — CRUD for own games with file uploads (multi-GB support)
- **Profile** — studio name, URL slug, description, website, logo, country
- **Reviews** — view reviews for own games
- **Sales** — create discount requests (price, date range), view history

---

## Real-Time Features

### SignalR Hubs

| Hub | Endpoint | Purpose |
|-----|----------|---------|
| `NotificationHub` | `/hub/notifications` | Real-time notifications, user connection tracking |

### Notification Types

| Category | Trigger | Recipient |
|----------|---------|-----------|
| `Friends` | Friend request sent/accepted | Target user |
| `Support` | New support ticket (admin broadcast) | All admins |
| `Support` | Admin reply / status change | Ticket owner |
| `System` | Role changes, account actions | Relevant user |

### Connection Tracker

`ConnectionTracker` singleton tracks:
- Online/offline status for friends list
- Last seen timestamps

### Notification Dropdown

Bell icon in the navbar shows unread notifications dynamically. Includes friend requests, system notifications, and support ticket updates. No separate notifications page — all interaction is through the dropdown.

---

## AutoMapper

**Package:** `AutoMapper.Extensions.Microsoft.DependencyInjection` v12.0.1

**Profile:** `Game-Store/Mappings/MappingProfile.cs`

Current mappings:
- `Post` → `PostViewModel` (used in ProfileController)
- `Review` → `ReviewDto` (used in HomeController)

Registered in `Program.cs`: `builder.Services.AddAutoMapper(typeof(MappingProfile));`

---

## Redis Caching

**Package:** `Microsoft.Extensions.Caching.StackExchangeRedis` v10.0.9

Redis replaces `AddDistributedMemoryCache()` for session storage:

```csharp
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});
```

Requires Redis server on `localhost:6379`. A startup batch file (`RedisServer.bat`) in the Windows Startup folder auto-starts Redis if not already running.

Without Redis, sessions are lost on app restart. The app still functions — users just need to re-login.

---

## File Uploads

### Storage Layout

```
wwwroot/uploads/
├── avatars/          ← User profile pictures
├── cvs/              ← Developer application CVs (PDF)
└── games/{gameId}/
    ├── images/       ← Cover images + screenshots
    └── files/        ← Game executables / archives
```

### Limits & Formats

| Setting | Value |
|---------|-------|
| Max file size | ~100 MB (configurable in Program.cs) |
| Accepted game formats | `.exe`, `.zip`, `.rar`, `.7z`, `.msi`, `.apk` |
| Accepted image formats | `.jpg`, `.jpeg`, `.png`, `.webp` |

---

## NuGet Packages

### GameStore.PL (Web App)

| Package | Version | Purpose |
|---------|---------|---------|
| `AutoMapper.Extensions.Microsoft.DependencyInjection` | 12.0.1 | Object mapping |
| `Microsoft.EntityFrameworkCore.Design` | 9.0.8 | EF Core migrations (design-time) |
| `Microsoft.Extensions.Caching.StackExchangeRedis` | 10.0.9 | Redis session cache |
| `Stripe.net` | 52.1.0 | Stripe payment processing |

### GameStore.BLL (Business Logic)

| Package | Version | Purpose |
|---------|---------|---------|
| `Microsoft.Extensions.Configuration.Abstractions` | 9.0.8 | Configuration access |
| `Microsoft.Extensions.Http` | 9.0.0 | HttpClientFactory |

### GameStore.DAL (Data Access)

| Package | Version | Purpose |
|---------|---------|---------|
| `BCrypt.Net-Next` | 4.0.3 | Password hashing |
| `Microsoft.EntityFrameworkCore` | 9.0.8 | ORM |
| `Microsoft.EntityFrameworkCore.SqlServer` | 9.0.8 | SQL Server provider |
| `Microsoft.EntityFrameworkCore.Tools` | 9.0.8 | Migrations CLI (design-time) |

### GameStore.Tests

| Package | Version | Purpose |
|---------|---------|---------|
| `xunit` | 2.9.3 | Test framework |
| `xunit.runner.visualstudio` | 3.1.4 | Test runner |
| `Microsoft.NET.Test.Sdk` | 17.14.1 | SDK |
| `FluentAssertions` | 6.12.2 | Assertion library |
| `Microsoft.EntityFrameworkCore.InMemory` | 9.0.8 | In-memory database provider |
| `Moq` | 4.20.72 | Mocking framework |

---

## Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "GameStoreConnection": "Server=(localdb)\\mssqllocaldb;Database=GameStoreDB;Trusted_Connection=True;MultipleActiveResultSets=true;",
    "Redis": "localhost:6379"
  },
  "AllowedHosts": "*",
  "Stripe": {
    "SecretKey": "",
    "PublishableKey": "",
    "WebhookSecret": ""
  },
  "PasswordSalt": "GameStoreSalt2026"
}
```

Stripe keys configured via **User Secrets** (Development) or environment variables (Production):
```bash
dotnet user-secrets set "Stripe:SecretKey" "sk_test_..."
dotnet user-secrets set "Stripe:PublishableKey" "pk_test_..."
dotnet user-secrets set "Stripe:WebhookSecret" "whsec_..."
```

---

## Testing

### Test Framework

xUnit + FluentAssertions + EF Core InMemory + Moq

### Test Pattern

All BLL service tests follow the same pattern:
1. Create unique in-memory DbContext (`UseInMemoryDatabase`)
2. Seed test data directly via DbContext
3. Create `UnitOfWork` and service-under-test
4. Execute method
5. Assert with FluentAssertions

```csharp
[Fact]
public async Task CreateAsync_Creates_Ticket_With_UserId()
{
    using var ctx = CreateContext("UniqueName");
    var uow = new UnitOfWork(ctx);
    var service = new SupportTicketService(uow);

    var ticket = await service.CreateAsync("u1", null, "Subject", "Message");

    ticket.UserId.Should().Be("u1");
    ticket.Status.Should().Be(TicketStatus.Open);
}
```

### Test Coverage

| Service | Tests | File |
|---------|-------|------|
| AuthService | ✓ | `AuthServiceTests.cs` |
| CartService | ✓ | `CartServiceTests.cs` |
| CategoryService | ✓ | `CategoryServiceTests.cs` |
| ChatService | ✓ | `ChatServiceTests.cs` |
| DeveloperApplicationService | ✓ | `DeveloperApplicationServiceTests.cs` |
| DeveloperService | ✓ | `DeveloperServiceTests.cs` |
| FriendService | ✓ | `FriendServiceTests.cs` |
| FriendSuggestionService | ✓ | `FriendSuggestionServiceTests.cs` |
| GameFileService | ✓ | `GameFileServiceTests.cs` |
| GameService | ✓ | `GameServiceTests.cs` |
| LibraryService | ✓ | `LibraryServiceTests.cs` |
| OrderAnalyticsService | ✓ | `OrderAnalyticsServiceTests.cs` |
| OrderService | ✓ | `OrderServiceTests.cs` |
| PostService | ✓ | `PostServiceTests.cs` |
| ReviewService | ✓ | `ReviewServiceTests.cs` |
| SaleService | ✓ | `SaleServiceTests.cs` |
| SeedService | ✓ | `SeedServiceTests.cs` |
| SupportTicketService | ✓ | `SupportTicketServiceTests.cs` |
| UserService | ✓ | `UserServiceTests.cs` |
| WishlistService | ✓ | `WishlistServiceTests.cs` |

**Total:** 20 test files covering all 20 BLL services + SeedService

### Running Tests

```bash
dotnet test
```

---

## Performance Notes

- **Read queries use `AsNoTracking()`** for display-only data (games, categories, pending sales)
- **Game JSON data is serialized once server-side**, not per-row in the Razor loop
- **Client-side search is debounced** at 150ms to avoid DOM thrashing
- **Session state uses Redis** (distributed cache) — persists across app restarts
- **Pending model changes warning suppressed** in development (`RelationalEventId.PendingModelChangesWarning`)
- **Posts have 30-second spam cooldown** — server-side check on `GetLastPostTimeAsync` + client-side countdown timer
- **Friend suggestion scoring** uses weighted algorithm (mutual games ×10, mutual friends ×7, wishlist overlap ×5, same developer ×4, same category ×3)

---

## Support Ticket Workflow

```
Open → InProgress → Resolved → Closed
```

- Users create tickets via the public Support form (authenticated = linked to user, anonymous = email only)
- Admins receive SignalR notification on new ticket
- Admins can reply, change status, and delete tickets
- Users receive SignalR notification on admin reply or status change
- Ticket replies include user info (with nullable support for deleted users — `SET NULL` on delete)

---

## Project Planning & Management

### Project Proposal

GameStore is a Steam-style digital game store built on ASP.NET Core 8 with role-based access control across three tiers: Customer, Developer, and Admin. The platform enables game browsing/purchasing, developer studio management, admin oversight, and social features (friends, chat, profile posts, notifications). Payment processing is handled via Stripe with support for both paid and free checkout flows. A sale/discount system with admin approval workflow allows developers to run promotions. A support ticket system provides structured customer support with reply and status tracking.

**Objectives:**
- Deliver a fully functional digital game marketplace with secure authentication
- Provide role-specific portals (Customer storefront, Developer dashboard, Admin panel)
- Implement real-time features (notifications, chat, online presence)
- Ensure maintainability through clean architecture (3-layer, DI, Repository/UoW)
- Achieve high test coverage across all business logic services

**Scope:**
- User registration, login, password reset, email change, profile management
- Game catalog with categories, search, pagination, hero carousel
- Shopping cart with Stripe payment integration
- Game library with ownership-based download
- Friend system with mutual-game-based suggestions
- Real-time messaging and notifications via SignalR
- Developer applications, studio profiles, game management, sales
- Admin dashboard with analytics (revenue, orders, top games)
- Support ticket system with admin reply and status workflow
- Profile posts with 30-second spam prevention cooldown

---

### Project Plan

| Phase | Milestone | Deliverables | Duration |
|-------|-----------|-------------|----------|
| **1. Foundation** | Project setup + data layer | Solution structure, EF Core entities, DbContext, migrations, Repository/UoW, GlobalUsings | Week 1 |
| **2. Auth & Users** | Authentication system | Session-based auth, login/register/password-reset, role management (ADMIN/CUSTOMER/DEVELOPER), `AdminOnlyFilter` + `DeveloperOnlyFilter` | Week 1-2 |
| **3. Game Catalog** | Storefront MVP | Game CRUD, categories (many-to-many), paginated listing, hero carousel, search/sort, game detail modal | Week 2-3 |
| **4. Commerce** | Cart + Checkout | Cart system, Stripe integration, free checkout, order management, game library with download | Week 3-4 |
| **5. Social** | Friends + Chat + Posts | Friend requests/accept/reject, mutual-game suggestions, real-time messaging, profile posts with 30s cooldown | Week 4-5 |
| **6. Developer Portal** | Developer area | Developer applications (approve/reject), studio profiles, game management, sales creation, revenue/downloads dashboard | Week 5-6 |
| **7. Admin Panel** | Admin area | Dashboard analytics (revenue by month, top games, orders by day), full game/category/user/review management, sale approval, developer management | Week 6-7 |
| **8. Notifications** | Real-time system | SignalR NotificationHub, ConnectionTracker, notification persistence, bell dropdown UI, admin broadcast, friend request events | Week 7 |
| **9. Support Tickets** | Support system | Ticket CRUD, user/admin reply, status workflow (Open→InProgress→Resolved→Closed), admin SignalR alerts | Week 7-8 |
| **10. Optimization** | Performance + Polish | Redis session caching, AutoMapper, debounced search, pre-serialized JSON data, code cleanup | Week 8 |
| **11. Testing** | Comprehensive coverage | xUnit test files for all 20+ BLL services, in-memory database tests, FluentAssertions, edge case coverage | Week 8-9 |
| **12. Documentation** | README + handover | Full project structure documentation, setup guide, route map, service layer reference, ERD | Week 9 |

---

### Task Assignment & Roles

| Role | Responsibilities | Team Member |
|------|-----------------|-------------|
| **Project Manager** | Timeline tracking, milestone reviews, requirement prioritization, risk management | Lead |
| **Backend Developer** | DAL entities, DbContext, migrations, Repository/UoW, all BLL services, controllers, filters, hubs | Developer |
| **Frontend Developer** | Views (cshtml), layouts, CSS/theming (cyberpunk), JavaScript (AJAX, SignalR client, search, modals), responsive design | Developer |
| **Database Administrator** | EF Core migrations, index optimization, constraint design, query performance | Developer |
| **DevOps** | Build pipeline, test automation, deployment configuration, Redis setup, Stripe key management | Developer |
| **QA / Tester** | xUnit test authoring, in-memory DB test patterns, edge case coverage, regression testing | Tester |
| **UI/UX Designer** | Cyberpunk theme (layout, color scheme, fonts, animations), component styling, responsive breakpoints | Designer |

---

### Risk Assessment & Mitigation Plan

| Risk | Probability | Impact | Mitigation |
|------|-----------|--------|-----------|
| **Session loss on app restart** | Medium | High — all users logged out | Redis distributed cache persists sessions across restarts; Startup folder batch file auto-starts Redis |
| **Stripe payment failures / webhook misses** | Low | High — lost revenue, broken orders | Webhook handler reconciles payment status; `CompleteCheckoutAsync` verifies session payment status server-side; manual order correction available in admin panel |
| **Database connection pooling exhaustion** | Low | Medium — site becomes unresponsive | `MultipleActiveResultSets=true` in connection string; `AsNoTracking()` for read queries; efficient pagination with `Skip`/`Take` |
| **Large file uploads causing OOM** | Low | Medium — server crash | 100 MB upload limit configured in Kestrel + FormOptions; uploads streamed to disk |
| **Spam / abuse (posts, reviews, tickets)** | Medium | Low — degraded UX | 30-second post cooldown (server + client); admin moderation for reviews and tickets; `SET NULL` on user delete preserves records while removing PII |
| **Concurrent cart modifications** | Low | Medium — inconsistent state | UNIQUE constraint on `CartItems(CartId, GameId)` prevents duplicates; per-user cart isolation |
| **Entity Framework N+1 queries** | Medium | Medium — slow page loads | Explicit `.Include()` / `.ThenInclude()` for all navigations; `AsNoTracking()` for read-only data; eager loading over lazy loading |
| **Stripe secret key exposure** | Low | Critical — financial fraud | Keys stored in User Secrets (dev) or environment variables (prod), never in source control; `appsettings.json` has empty placeholder values |

---

### KPIs (Key Performance Indicators)

| KPI | Target | Measurement |
|-----|--------|-------------|
| **System Uptime** | ≥ 99.5% | Server monitoring / health checks |
| **Session Persistence** | 100% across restarts | Redis cache hit rate ≥ 99% |
| **Page Load Time (storefront)** | ≤ 2 seconds | Browser DevTools / Lighthouse |
| **API Response Time (AJAX)** | ≤ 500ms | Server-side logging / browser timing |
| **Checkout Success Rate** | ≥ 95% | Completed orders ÷ initiated checkouts |
| **Test Coverage (BLL services)** | 100% | xUnit test count / service method count |
| **Build Success Rate** | 100% | CI pipeline pass/fail |
| **Spam Prevention Effectiveness** | 100% | No duplicate posts within 30s window |
| **Database Query Performance** | ≤ 100ms per query | EF Core logging / SQL Server profiler |
| **User Registration Conversion** | ≥ 20% | Registered users ÷ unique visitors |
| **Support Ticket Resolution Time** | ≤ 48 hours | Average time from Open to Resolved/Closed |
| **Notification Delivery Latency** | ≤ 1 second | SignalR round-trip timing |
