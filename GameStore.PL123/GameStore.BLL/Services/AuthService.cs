using System.Security.Cryptography;
using System.Text;

namespace GameStore.BLL.Services
{
    public class AuthService
    {
        private readonly GameStoreDbContext _context;

        public AuthService(GameStoreDbContext context)
        {
            _context = context;
        }

        public static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public async Task<User?> LoginAsync(string email, string password)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null) return null;

            var storedHash = user.PasswordHash;

            if (storedHash.StartsWith("$2"))
            {
                if (!BCrypt.Net.BCrypt.Verify(password, storedHash))
                    return null;
            }
            else
            {
                using var sha = SHA256.Create();
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password + "GameStoreSalt2026"));
                var legacyHash = Convert.ToBase64String(bytes);

                if (storedHash != legacyHash)
                    return null;

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
                await _context.SaveChangesAsync();
            }

            return user;
        }

        public async Task<(bool Success, string Error)> RegisterAsync(string username, string email, string password, Role role = Role.CUSTOMER)
        {
            if (await _context.Users.AnyAsync(u => u.Email == email))
                return (false, "Email already registered.");

            var user = new User
            {
                Username = username,
                Email = email,
                PasswordHash = HashPassword(password),
                Role = role
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return (true, string.Empty);
        }
    }
}
