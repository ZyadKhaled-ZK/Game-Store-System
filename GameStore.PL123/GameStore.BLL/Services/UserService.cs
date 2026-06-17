namespace GameStore.BLL.Services
{
    public class UserService : IUserService
    {
        private readonly GameStoreDbContext _context;

        public UserService(GameStoreDbContext context)
        {
            _context = context;
        }

        public async Task<List<User>> GetAllAsync()
        {
            return await _context.Users
                .OrderBy(u => u.Role)
                .ThenBy(u => u.Username)
                .ToListAsync();
        }

        public async Task<User?> GetByIdAsync(string id)
        {
            return await _context.Users.FindAsync(id);
        }

        public async Task<(bool Success, string Error)> ChangeRoleAsync(string id, Role newRole, string? currentUserId = null)
        {
            if (id == currentUserId)
                return (false, "Cannot change your own role.");

            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return (false, "User not found.");

            user.Role = newRole;
            await _context.SaveChangesAsync();
            return (true, string.Empty);
        }

        public async Task<(bool Success, string Error)> DeleteAsync(string id, string? currentUserId = null)
        {
            if (id == currentUserId)
                return (false, "Cannot delete your own account.");

            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return (false, "User not found.");

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return (true, string.Empty);
        }

        public async Task<int> GetTotalUsersAsync()
        {
            return await _context.Users.CountAsync();
        }
    }
}
