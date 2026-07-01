using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace GameStore.BLL.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _uow;
        private readonly string _salt;

        public AuthService(IUnitOfWork uow, IConfiguration configuration)
        {
            _uow = uow;
            _salt = configuration["PasswordSalt"] ?? "GameStoreSalt2026";
        }

        public static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public async Task<User?> LoginAsync(string email, string password)
        {
            var user = await _uow.Repository<User>().FirstOrDefaultAsync(u => u.Email == email);

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
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password + _salt));
                var legacyHash = Convert.ToBase64String(bytes);

                if (storedHash != legacyHash)
                    return null;

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
                await _uow.SaveChangesAsync();
            }

            return user;
        }

        public async Task<(bool Success, string Error)> RegisterAsync(string username, string email, string password, Role role = Role.CUSTOMER)
        {
            if (await _uow.Repository<User>().AnyAsync(u => u.Email == email))
                return (false, "Email already registered.");

            var user = new User
            {
                Username = username,
                Email = email,
                PasswordHash = HashPassword(password),
                Role = role
            };

            await _uow.Repository<User>().AddAsync(user);
            await _uow.SaveChangesAsync();
            return (true, string.Empty);
        }

        public async Task<(bool Success, string Error)> ChangePasswordAsync(string userId, string currentPassword, string newPassword)
        {
            var user = await _uow.Repository<User>().GetByIdAsync(userId);
            if (user == null) return (false, "User not found.");

            var storedHash = user.PasswordHash;
            if (storedHash.StartsWith("$2"))
            {
                if (!BCrypt.Net.BCrypt.Verify(currentPassword, storedHash))
                    return (false, "Current password is incorrect.");
            }
            else
            {
                using var sha = SHA256.Create();
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(currentPassword + _salt));
                var legacyHash = Convert.ToBase64String(bytes);
                if (storedHash != legacyHash)
                    return (false, "Current password is incorrect.");
            }

            user.PasswordHash = HashPassword(newPassword);
            await _uow.SaveChangesAsync();
            return (true, string.Empty);
        }

        public async Task<(bool Success, string Error, string? Token)> GenerateResetTokenAsync(string email)
        {
            var user = await _uow.Repository<User>().FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return (false, "If that email exists, a reset link has been sent.", null);

            var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            await _uow.Repository<PasswordResetToken>().AddAsync(new PasswordResetToken
            {
                UserId = user.Id,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            });
            await _uow.SaveChangesAsync();
            return (true, string.Empty, token);
        }

        public async Task<(bool Success, string Error)> ResetPasswordAsync(string token, string newPassword)
        {
            var resetToken = await _uow.Repository<PasswordResetToken>()
                .FirstOrDefaultAsync(t => t.Token == token && !t.IsUsed && t.ExpiresAt > DateTime.UtcNow);

            if (resetToken == null)
                return (false, "Invalid or expired reset token.");

            var user = await _uow.Repository<User>().GetByIdAsync(resetToken.UserId);
            if (user == null)
                return (false, "User not found.");

            user.PasswordHash = HashPassword(newPassword);
            resetToken.IsUsed = true;
            await _uow.SaveChangesAsync();
            return (true, string.Empty);
        }

        public async Task<(User? User, bool IsNew)> GetOrCreateExternalUserAsync(string email, string username, string? avatarUrl)
        {
            var user = await _uow.Repository<User>().FirstOrDefaultAsync(u => u.Email == email);
            if (user != null) return (user, false);

            var baseUsername = username;
            int suffix = 1;
            while (await _uow.Repository<User>().AnyAsync(u => u.Username == username))
            {
                username = $"{baseUsername}{suffix}";
                suffix++;
            }

            user = new User
            {
                Username = username,
                Email = email,
                AvatarUrl = avatarUrl,
                PasswordHash = HashPassword(Guid.NewGuid().ToString()),
                Role = Role.CUSTOMER,
                IsExternalAccount = true
            };

            await _uow.Repository<User>().AddAsync(user);
            await _uow.SaveChangesAsync();
            return (user, true);
        }

        public async Task<bool> IsExternalAccountAsync(string email)
        {
            var user = await _uow.Repository<User>().FirstOrDefaultAsync(u => u.Email == email);
            return user?.IsExternalAccount ?? false;
        }

        public async Task<(bool Success, string Error)> UpdateProfileAsync(string userId, string username, string? bio)
        {
            var user = await _uow.Repository<User>().GetByIdAsync(userId);
            if (user == null) return (false, "User not found.");

            if (user.Username != username && await _uow.Repository<User>().AnyAsync(u => u.Username == username))
                return (false, "Username already taken.");

            user.Username = username;
            user.Bio = bio;
            await _uow.SaveChangesAsync();
            return (true, string.Empty);
        }

        public async Task<bool> UsernameExistsAsync(string username)
        {
            return await _uow.Repository<User>().AnyAsync(u => u.Username == username);
        }

        public async Task<(bool Success, string Error)> UpdateEmailAsync(string userId, string newEmail)
        {
            if (await _uow.Repository<User>().AnyAsync(u => u.Email == newEmail && u.Id != userId))
                return (false, "Email already in use.");

            var user = await _uow.Repository<User>().GetByIdAsync(userId);
            if (user == null) return (false, "User not found.");

            user.Email = newEmail;
            await _uow.SaveChangesAsync();
            return (true, string.Empty);
        }
    }
}
