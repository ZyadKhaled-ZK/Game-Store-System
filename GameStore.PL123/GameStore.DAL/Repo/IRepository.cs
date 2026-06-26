namespace GameStore.DAL.Repo
{
    public interface IRepository<T> where T : class
    {
        IQueryable<T> Query();
        Task<List<T>> GetAllAsync();
        Task<T?> GetByIdAsync(string id);
        Task AddAsync(T entity);
        void Update(T entity);
        void Delete(T entity);
        void RemoveRange(IEnumerable<T> entities);
        Task<bool> AnyAsync(Expression<Func<T, bool>>? predicate = null);
        Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);
        Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
    }
}
