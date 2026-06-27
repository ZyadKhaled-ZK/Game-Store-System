namespace GameStore.DAL.Repo
{

    public class EfRepository<T> : IRepository<T> where T : class
    {
        private readonly DbSet<T> _dbSet;

        public EfRepository(GameStoreDbContext context) => _dbSet = context.Set<T>();

        public IQueryable<T> Query() => _dbSet.AsQueryable();

        public async Task<List<T>> GetAllAsync() => await _dbSet.ToListAsync();

        public async Task<T?> GetByIdAsync(string id) => await _dbSet.FindAsync(id);

        public async Task AddAsync(T entity) => await _dbSet.AddAsync(entity);

        public void Update(T entity) => _dbSet.Update(entity);

        public void Delete(T entity) => _dbSet.Remove(entity);

        public void RemoveRange(IEnumerable<T> entities) => _dbSet.RemoveRange(entities);

        public async Task<bool> AnyAsync(Expression<Func<T, bool>>? predicate = null)
            => predicate == null ? await _dbSet.AnyAsync() : await _dbSet.AnyAsync(predicate);

        public async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
            => predicate == null ? await _dbSet.CountAsync() : await _dbSet.CountAsync(predicate);

        public async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
            => await _dbSet.FirstOrDefaultAsync(predicate);
    }
}
