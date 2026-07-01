namespace GameStore.BLL.Services
{
    public class SeedService
    {
        private readonly IUnitOfWork _uow;

        public SeedService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task SeedAsync()
        {
            if (!await _uow.Repository<Category>().AnyAsync())
            {
                var categories = new[]
                {
                    "Action", "Adventure", "RPG", "Strategy", "Simulation",
                    "Sports", "Racing", "Indie", "Puzzle", "Horror",
                    "Shooter", "Fighting"
                };
                foreach (var name in categories)
                    await _uow.Repository<Category>().AddAsync(new Category { Name = name });
                await _uow.SaveChangesAsync();
            }

            if (!await _uow.Repository<User>().AnyAsync(u => u.Role == Role.DEVELOPER))
            {
                await _uow.Repository<User>().AddAsync(new User
                {
                    Username = "Developer",
                    Email = "dev@gamestore.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Dev@1234"),
                    Role = Role.DEVELOPER
                });
                await _uow.SaveChangesAsync();
            }

            if (!await _uow.Repository<User>().AnyAsync(u => u.Role == Role.ADMIN))
            {
                await _uow.Repository<User>().AddAsync(new User
                {
                    Username = "Admin",
                    Email = "admin@gamestore.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@1234"),
                    Role = Role.ADMIN
                });
                await _uow.SaveChangesAsync();
            }
        }
    }
}
