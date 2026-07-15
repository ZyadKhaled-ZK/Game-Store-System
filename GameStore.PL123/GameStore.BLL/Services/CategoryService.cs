namespace GameStore.BLL.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly IUnitOfWork _uow;

        public CategoryService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<List<Category>> GetAllAsync()
        {
            return await _uow.Repository<Category>().Query()
                .OrderBy(c => c.Name)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<Category>> GetAllWithGameCountAsync()
        {
            return await _uow.Repository<Category>().Query()
                .Include(c => c.GameCategories)
                .OrderBy(c => c.Name)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<(bool Success, string Error)> CreateAsync(string name)
        {
            if (await _uow.Repository<Category>().AnyAsync(c => c.Name == name))
                return (false, $"Category '{name}' already exists.");

            await _uow.Repository<Category>().AddAsync(new Category { Name = name });
            await _uow.SaveChangesAsync();
            return (true, string.Empty);
        }

        public async Task<bool> UpdateAsync(string id, string name)
        {
            var cat = await _uow.Repository<Category>().GetByIdAsync(id);
            if (cat == null) return false;

            cat.Name = name;
            await _uow.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var cat = await _uow.Repository<Category>().Query()
                .Include(c => c.GameCategories)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cat == null || cat.GameCategories.Any()) return false;

            _uow.Repository<Category>().Delete(cat);
            await _uow.SaveChangesAsync();
            return true;
        }
    }
}
