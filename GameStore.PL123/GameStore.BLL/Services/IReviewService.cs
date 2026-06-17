namespace GameStore.BLL.Services
{
    public interface IReviewService
    {
        Task<List<Review>> GetAllWithDetailsAsync();
        Task<bool> DeleteAsync(string id);
    }
}
