namespace GameStore.BLL.Services
{
    public interface IUserService
    {
        Task<List<User>> GetAllAsync();
        Task<User?> GetByIdAsync(string id);
        Task<(bool Success, string Error)> ChangeRoleAsync(string id, Role newRole, string? currentUserId = null);
        Task<(bool Success, string Error)> DeleteAsync(string id, string? currentUserId = null);
        Task<int> GetTotalUsersAsync();
    }
}
