using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

namespace GameStore.BLL.Services
{
    public class AuthService : IAuthService
    {
        public static (bool IsValid, string Error) ValidatePassword(string password, string? email = null)
        {
            if (string.IsNullOrEmpty(password))
                return (false, "Password is required.");

            if (!string.IsNullOrEmpty(email))
            {
                var emailLocal = email.Split('@')[0];
                if (string.Equals(password, email, StringComparison.OrdinalIgnoreCase))
                    return (false, "Password cannot be the same as your email.");

                if (string.Equals(password, emailLocal, StringComparison.OrdinalIgnoreCase))
                    return (false, "Password cannot be the same as your email.");
            }

            if (password.Length < 8)
                return (false, "Password must be at least 8 characters long.");

            if (!Regex.IsMatch(password, @"\d"))
                return (false, "Password must contain at least one number.");

            if (!Regex.IsMatch(password, @"[^\w\s]"))
                return (false, "Password must contain at least one special character.");

            return (true, string.Empty);
        }

        public static (bool IsValid, string Error) ValidateUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return (false, "Username is required.");

            if (username.Length < 4)
                return (false, "Username must be at least 4 characters long.");

            if (username.Length > 20)
                return (false, "Username must not exceed 20 characters.");

            if (!Regex.IsMatch(username, @"^[a-zA-Z0-9]+$"))
                return (false, "Username can only contain letters and numbers.");

            return (true, string.Empty);
        }
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
            var usernameValidation = ValidateUsername(username);
            if (!usernameValidation.IsValid)
                return (false, usernameValidation.Error);

            var passwordValidation = ValidatePassword(password, email);
            if (!passwordValidation.IsValid)
                return (false, passwordValidation.Error);

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

            var passwordValidation = ValidatePassword(newPassword, user.Email);
            if (!passwordValidation.IsValid)
                return (false, passwordValidation.Error);

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

            var passwordValidation = ValidatePassword(newPassword, user.Email);
            if (!passwordValidation.IsValid)
                return (false, passwordValidation.Error);

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
            var usernameValidation = ValidateUsername(username);
            if (!usernameValidation.IsValid)
                return (false, usernameValidation.Error);

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

        public async Task<(bool Success, string Error)> ConfirmEmailAsync(string userId)
        {
            var user = await _uow.Repository<User>().GetByIdAsync(userId);
            if (user == null) return (false, "User not found.");

            user.EmailConfirmed = true;
            await _uow.SaveChangesAsync();
            return (true, string.Empty);
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

        public async Task<User?> FindUserByEmailAsync(string email)
        {
            return await _uow.Repository<User>().FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<string> CreateVerificationTokenAsync(string userId)
        {
            var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            var entity = new EmailVerificationToken
            {
                UserId = userId,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };
            await _uow.Repository<EmailVerificationToken>().AddAsync(entity);
            await _uow.SaveChangesAsync();
            return token;
        }

        public async Task<(bool Success, string? UserId)> ConsumeVerificationTokenAsync(string token)
        {
            var entity = await _uow.Repository<EmailVerificationToken>()
                .FirstOrDefaultAsync(t => t.Token == token && !t.IsUsed && t.ExpiresAt > DateTime.UtcNow);

            if (entity == null)
                return (false, null);

            entity.IsUsed = true;
            await _uow.SaveChangesAsync();
            return (true, entity.UserId);
        }

        public async Task InvalidateUserTokensAsync(string userId)
        {
            var tokens = await _uow.Repository<EmailVerificationToken>().Query()
                .Where(t => t.UserId == userId && !t.IsUsed)
                .ToListAsync();

            foreach (var t in tokens)
                t.IsUsed = true;

            await _uow.SaveChangesAsync();
        }
    }
}
