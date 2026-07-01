using GameStore.DAL.Entities;

namespace GameStore.BLL.Services;

public interface IDeveloperApplicationService
{
    Task<DeveloperApplication?> GetByIdAsync(string id);
    Task<DeveloperApplication?> GetByUserIdAsync(string userId);
    Task<List<DeveloperApplication>> GetAllAsync();
    Task<(bool Success, string Error)> SubmitAsync(string userId, string name, string? description, string? website, string? country, string? cvFilePath = null, string? githubUrl = null);
    Task<(bool Success, string Error)> ApproveAsync(string applicationId);
    Task<(bool Success, string Error)> RejectAsync(string applicationId);
}
