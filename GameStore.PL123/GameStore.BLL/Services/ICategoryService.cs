namespace GameStore.BLL.Services
{
    public interface ICategoryService
    {
        Task<List<Category>> GetAllAsync();
    Task<List<Category>> GetAllWithGameCountAsync();
        Task<(bool Success, string Error)> CreateAsync(string name);
        Task<bool> UpdateAsync(string id, string name);
        Task<bool> DeleteAsync(string id);
    }
}
