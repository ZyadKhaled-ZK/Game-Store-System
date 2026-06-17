namespace GameStore.BLL.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly GameStoreDbContext _context;

        public CategoryService(GameStoreDbContext context)
        {
            _context = context;
        }

        public async Task<List<Category>> GetAllAsync()
        {
            return await _context.Categories
                .OrderBy(c => c.Name)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<Category>> GetAllWithGameCountAsync()
        {
            return await _context.Categories
                .Include(c => c.GameCategories)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<(bool Success, string Error)> CreateAsync(string name)
        {
            if (await _context.Categories.AnyAsync(c => c.Name == name))
                return (false, $"Category '{name}' already exists.");

            _context.Categories.Add(new Category { Name = name });
            await _context.SaveChangesAsync();
            return (true, string.Empty);
        }

        public async Task<bool> UpdateAsync(string id, string name)
        {
            var cat = await _context.Categories.FindAsync(id);
            if (cat == null) return false;

            cat.Name = name;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var cat = await _context.Categories
                .Include(c => c.GameCategories)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cat == null || cat.GameCategories.Any()) return false;

            _context.Categories.Remove(cat);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
