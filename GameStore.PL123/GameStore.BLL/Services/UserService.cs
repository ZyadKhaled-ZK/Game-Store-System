using GameStore.BLL.Models;

namespace GameStore.BLL.Services
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _uow;

        public UserService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<List<User>> GetAllAsync()
        {
            return await _uow.Repository<User>().Query()
                .OrderBy(u => u.Role)
                .ThenBy(u => u.Username)
                .ToListAsync();
        }

        public async Task<User?> GetByIdAsync(string id)
        {
            return await _uow.Repository<User>().GetByIdAsync(id);
        }

        public async Task<(bool Success, string Error)> ChangeRoleAsync(string id, Role newRole, string? currentUserId = null)
        {
            if (id == currentUserId)
                return (false, "Cannot change your own role.");

            var user = await _uow.Repository<User>().GetByIdAsync(id);
            if (user == null)
                return (false, "User not found.");

            user.Role = newRole;
            await _uow.SaveChangesAsync();
            return (true, string.Empty);
        }

        public async Task<(bool Success, string Error)> DeleteAsync(string id, string? currentUserId = null)
        {
            if (id == currentUserId)
                return (false, "Cannot delete your own account.");

            var user = await _uow.Repository<User>().GetByIdAsync(id);
            if (user == null)
                return (false, "User not found.");

            _uow.Repository<User>().Delete(user);
            await _uow.SaveChangesAsync();
            return (true, string.Empty);
        }

        public async Task<int> GetTotalUsersAsync()
        {
            return await _uow.Repository<User>().CountAsync();
        }

        public async Task<List<UsersByRole>> GetUsersByRoleAsync()
        {
            return await _uow.Repository<User>().Query()
                .GroupBy(u => u.Role)
                .Select(g => new UsersByRole
                {
                    Role = g.Key.ToString(),
                    Count = g.Count()
                })
                .ToListAsync();
        }

        public async Task<List<UsersByMonth>> GetUsersByMonthAsync(int months = 12)
        {
            var since = DateTime.UtcNow.AddMonths(-months);
            return await _uow.Repository<User>().Query()
                .Where(u => u.CreatedAt >= since)
                .GroupBy(u => new { u.CreatedAt.Year, u.CreatedAt.Month })
                .Select(g => new UsersByMonth
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Count = g.Count()
                })
                .OrderBy(r => r.Year).ThenBy(r => r.Month)
                .ToListAsync();
        }
    }
}
