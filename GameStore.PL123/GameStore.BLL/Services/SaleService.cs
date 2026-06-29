using Microsoft.EntityFrameworkCore;

namespace GameStore.BLL.Services
{
    public class SaleService : ISaleService
    {
        private readonly IUnitOfWork _uow;

        public SaleService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<Sale?> GetByIdAsync(string id)
        {
            return await _uow.Repository<Sale>().Query()
                .Include(s => s.Game)
                .Include(s => s.Developer)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<List<Sale>> GetPendingAsync()
        {
            return await _uow.Repository<Sale>().Query()
                .Include(s => s.Game)
                .Include(s => s.Developer)
                .Where(s => s.Status == SaleStatus.Pending)
                .OrderByDescending(s => s.CreatedAt)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<Sale>> GetByDeveloperAsync(string developerId)
        {
            return await _uow.Repository<Sale>().Query()
                .Include(s => s.Game)
                .Where(s => s.DeveloperId == developerId)
                .OrderByDescending(s => s.CreatedAt)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<Sale>> GetActiveSalesByGameIdsAsync(List<string> gameIds)
        {
            if (gameIds == null || gameIds.Count == 0)
                return new List<Sale>();

            var now = DateTime.UtcNow;
            return await _uow.Repository<Sale>().Query()
                .Where(s => gameIds.Contains(s.GameId)
                    && s.Status == SaleStatus.Approved
                    && s.StartDate <= now
                    && s.EndDate >= now)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<(bool Success, string Error)> CreateAsync(string developerId, string gameId, decimal newPrice, DateTime startDate, DateTime endDate)
        {
            if (newPrice < 0)
                return (false, "Price cannot be negative.");

            if (endDate <= startDate)
                return (false, "End date must be after start date.");

            if (endDate <= DateTime.UtcNow)
                return (false, "End date must be in the future.");

            if (newPrice < 0.01m)
                return (false, "Sale price must be at least $0.01.");

            var game = await _uow.Repository<Game>().GetByIdAsync(gameId);
            if (game == null)
                return (false, "Game not found.");

            if (game.DeveloperId != developerId)
                return (false, "You can only create sales for your own games.");

            if (newPrice >= game.Price)
                return (false, "Sale price must be less than the original price.");

            await _uow.BeginTransactionAsync();
            try
            {
                var existingPending = await _uow.Repository<Sale>().Query()
                    .FirstOrDefaultAsync(s => s.GameId == gameId
                        && s.DeveloperId == developerId
                        && s.Status == SaleStatus.Pending);

                if (existingPending != null)
                {
                    existingPending.Status = SaleStatus.Cancelled;
                    _uow.Repository<Sale>().Update(existingPending);
                }

                var sale = new Sale
                {
                    GameId = gameId,
                    DeveloperId = developerId,
                    NewPrice = newPrice,
                    StartDate = startDate,
                    EndDate = endDate,
                    Status = SaleStatus.Pending
                };

                await _uow.Repository<Sale>().AddAsync(sale);
                await _uow.SaveChangesAsync();
                await _uow.CommitAsync();

                return (true, "Sale request submitted for admin approval.");
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }
        }

        public async Task<(bool Success, string Error)> ApproveAsync(string id)
        {
            var sale = await _uow.Repository<Sale>().GetByIdAsync(id);
            if (sale == null)
                return (false, "Sale not found.");

            if (sale.Status != SaleStatus.Pending)
                return (false, "Only pending sales can be approved.");

            sale.Status = SaleStatus.Approved;
            sale.ApprovedAt = DateTime.UtcNow;

            _uow.Repository<Sale>().Update(sale);
            await _uow.SaveChangesAsync();

            return (true, "Sale approved successfully.");
        }

        public async Task<(bool Success, string Error)> RejectAsync(string id, string reason)
        {
            var sale = await _uow.Repository<Sale>().GetByIdAsync(id);
            if (sale == null)
                return (false, "Sale not found.");

            if (sale.Status != SaleStatus.Pending)
                return (false, "Only pending sales can be rejected.");

            sale.Status = SaleStatus.Rejected;
            sale.RejectReason = reason;

            _uow.Repository<Sale>().Update(sale);
            await _uow.SaveChangesAsync();

            return (true, "Sale rejected.");
        }
    }
}
