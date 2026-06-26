namespace GameStore.BLL.Services
{
    public interface IAuthService
    {
        Task<User?> LoginAsync(string email, string password);
        Task<(bool Success, string Error)> RegisterAsync(string username, string email, string password, Role role = Role.CUSTOMER);
        Task<(bool Success, string Error)> ChangePasswordAsync(string userId, string currentPassword, string newPassword);
        Task<(bool Success, string Error, string? Token)> GenerateResetTokenAsync(string email);
        Task<(bool Success, string Error)> ResetPasswordAsync(string token, string newPassword);
        Task<(bool Success, string Error)> UpdateEmailAsync(string userId, string newEmail);
    }
}
