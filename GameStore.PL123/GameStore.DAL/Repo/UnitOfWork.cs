namespace GameStore.DAL.Repo
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly GameStoreDbContext _context;
        private readonly Dictionary<Type, object> _repositories = new();
        private IDbContextTransaction? _transaction;

        public UnitOfWork(GameStoreDbContext context) => _context = context;

        public IRepository<T> Repository<T>() where T : class
        {
            var type = typeof(T);
            if (!_repositories.ContainsKey(type))
                _repositories[type] = new EfRepository<T>(_context);
            return (IRepository<T>)_repositories[type];
        }

        public async Task<int> SaveChangesAsync() => await _context.SaveChangesAsync();

        public async Task BeginTransactionAsync()
            => _transaction = await _context.Database.BeginTransactionAsync();

        public async Task CommitAsync()
        {
            if (_transaction != null) await _transaction.CommitAsync();
        }

        public async Task RollbackAsync()
        {
            if (_transaction != null) await _transaction.RollbackAsync();
        }

        public void Dispose() => _transaction?.Dispose();
    }
}
