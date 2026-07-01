using Microsoft.EntityFrameworkCore;

namespace GameStore.BLL.Services
{
    public class DeveloperService : IDeveloperService
    {
        private readonly IUnitOfWork _uow;

        public DeveloperService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<Developer?> GetByUserIdAsync(string userId)
        {
            return await _uow.Repository<Developer>().Query()
                .FirstOrDefaultAsync(d => d.UserId == userId);
        }

        public async Task<Developer?> GetByIdAsync(string id)
        {
            return await _uow.Repository<Developer>().GetByIdAsync(id);
        }

        public async Task<List<Developer>> GetAllAsync()
        {
            return await _uow.Repository<Developer>().Query()
                .OrderBy(d => d.Name)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<Game>> GetGamesAsync(string developerId)
        {
            return await _uow.Repository<Game>().Query()
                .Where(g => g.DeveloperId == developerId)
                .Include(g => g.GameCategories).ThenInclude(gc => gc.Category)
                .OrderByDescending(g => g.CreatedAt)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<(bool Success, string Error)> CreateOrUpdateProfileAsync(string userId, string name, string? slug, string? description, string? website, string? logoUrl, string? country)
        {
            var existing = await _uow.Repository<Developer>().Query()
                .FirstOrDefaultAsync(d => d.UserId == userId);

            if (existing != null)
            {
                existing.Name = name;
                existing.Slug = slug;
                existing.Description = description;
                existing.Website = website;
                existing.LogoUrl = logoUrl;
                existing.Country = country;
                _uow.Repository<Developer>().Update(existing);
            }
            else
            {
                if (string.IsNullOrEmpty(slug))
                    slug = name.ToLower().Replace(" ", "-");

                slug = await EnsureUniqueSlugAsync(slug);

                await _uow.Repository<Developer>().AddAsync(new Developer
                {
                    Name = name,
                    Slug = slug,
                    Description = description,
                    Website = website,
                    LogoUrl = logoUrl,
                    Country = country,
                    UserId = userId
                });
            }

            await _uow.SaveChangesAsync();
            return (true, string.Empty);
        }

        private async Task<string> EnsureUniqueSlugAsync(string slug)
        {
            if (!await _uow.Repository<Developer>().AnyAsync(d => d.Slug == slug))
                return slug;

            for (var i = 1; i <= 999; i++)
            {
                var candidate = $"{slug}-{i}";
                if (!await _uow.Repository<Developer>().AnyAsync(d => d.Slug == candidate))
                    return candidate;
            }

            return $"{slug}-{Guid.NewGuid():N}";
        }

        public async Task<(int GameCount, int TotalDownloads, int TotalReviews, int TotalRevenue, double AvgRating)> GetDashboardStatsAsync(string developerId)
        {
            var games = await _uow.Repository<Game>().Query()
                .Where(g => g.DeveloperId == developerId)
                .Include(g => g.LibraryGames)
                .Include(g => g.Reviews)
                .Include(g => g.OrderItems)
                .AsNoTracking()
                .ToListAsync();

            var gameCount = games.Count;
            var totalDownloads = games.Sum(g => g.LibraryGames.Count);
            var totalReviews = games.Sum(g => g.Reviews.Count);
            var totalRevenue = games.Sum(g => g.OrderItems.Sum(oi => oi.PriceAtPurchase));
            var avgRating = games.SelectMany(g => g.Reviews)
                .DefaultIfEmpty()
                .Average(r => r?.Rating ?? 0);

            return (gameCount, totalDownloads, totalReviews, (int)totalRevenue, Math.Round(avgRating, 1));
        }

        public async Task<List<(Game Game, int Downloads, double AvgRating, int ReviewCount)>> GetGameStatsAsync(string developerId)
        {
            var games = await _uow.Repository<Game>().Query()
                .Where(g => g.DeveloperId == developerId)
                .Include(g => g.LibraryGames)
                .Include(g => g.Reviews)
                .Include(g => g.GameCategories).ThenInclude(gc => gc.Category)
                .OrderByDescending(g => g.CreatedAt)
                .AsNoTracking()
                .ToListAsync();

            return games.Select(g => (
                g,
                Downloads: g.LibraryGames.Count,
                AvgRating: g.Reviews.Any() ? Math.Round(g.Reviews.Average(r => r.Rating), 1) : 0.0,
                ReviewCount: g.Reviews.Count
            )).ToList();
        }

        public async Task<bool> IsDeveloperUserAsync(string userId)
        {
            return await _uow.Repository<Developer>().AnyAsync(d => d.UserId == userId);
        }

        public async Task<(bool Success, string Error)> DeleteAsync(string developerId)
        {
            var dev = await _uow.Repository<Developer>().GetByIdAsync(developerId);
            if (dev == null)
                return (false, "Developer not found.");

            _uow.Repository<Developer>().Delete(dev);
            await _uow.SaveChangesAsync();
            return (true, string.Empty);
        }

        public async Task<(bool Success, string Error)> DemoteAsync(string developerId, string? currentUserId = null)
        {
            var dev = await _uow.Repository<Developer>().Query()
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.Id == developerId);

            if (dev == null)
                return (false, "Developer not found.");

            if (dev.UserId == currentUserId)
                return (false, "Cannot demote yourself.");

            if (dev.User != null)
            {
                dev.User.Role = Role.CUSTOMER;
                _uow.Repository<User>().Update(dev.User);
            }

            dev.IsActive = false;
            _uow.Repository<Developer>().Update(dev);
            await _uow.SaveChangesAsync();
            return (true, string.Empty);
        }

        public async Task<(bool Success, string Error)> ReactivateAsync(string developerId, string? currentUserId = null)
        {
            var dev = await _uow.Repository<Developer>().Query()
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.Id == developerId);

            if (dev == null)
                return (false, "Developer not found.");

            if (dev.UserId == currentUserId)
                return (false, "Cannot reactivate yourself.");

            dev.IsActive = true;
            _uow.Repository<Developer>().Update(dev);

            if (dev.User != null)
            {
                dev.User.Role = Role.DEVELOPER;
                _uow.Repository<User>().Update(dev.User);
            }

            await _uow.SaveChangesAsync();
            return (true, string.Empty);
        }
    }
}
