using Microsoft.EntityFrameworkCore;

namespace GameStore.BLL.Services;

public class DeveloperApplicationService : IDeveloperApplicationService
{
    private readonly IUnitOfWork _uow;
    private readonly IDeveloperService _devService;
    private readonly IUserService _userService;

    public DeveloperApplicationService(IUnitOfWork uow, IDeveloperService devService, IUserService userService)
    {
        _uow = uow;
        _devService = devService;
        _userService = userService;
    }

    public async Task<DeveloperApplication?> GetByIdAsync(string id)
    {
        return await _uow.Repository<DeveloperApplication>().Query()
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<DeveloperApplication?> GetByUserIdAsync(string userId)
    {
        return await _uow.Repository<DeveloperApplication>().Query()
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.UserId == userId && a.Status == ApplicationStatus.Pending);
    }

    public async Task<List<DeveloperApplication>> GetAllAsync()
    {
        return await _uow.Repository<DeveloperApplication>().Query()
            .Include(a => a.User)
            .OrderByDescending(a => a.CreatedAt)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<DeveloperApplication>> GetPendingAsync()
    {
        return await _uow.Repository<DeveloperApplication>().Query()
            .Include(a => a.User)
            .Where(a => a.Status == ApplicationStatus.Pending)
            .OrderByDescending(a => a.CreatedAt)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<(bool Success, string Error)> SubmitAsync(string userId, string name, string? description, string? website, string? country, string? cvFilePath = null, string? githubUrl = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            return (false, "Studio name is required.");

        var existing = await _uow.Repository<DeveloperApplication>().Query()
            .FirstOrDefaultAsync(a => a.UserId == userId && a.Status == ApplicationStatus.Pending);

        if (existing != null)
            return (false, "You already have a pending application.");

        if (await _devService.IsDeveloperUserAsync(userId))
            return (false, "You are already a developer.");

        await _uow.Repository<DeveloperApplication>().AddAsync(new DeveloperApplication
        {
            UserId = userId,
            Name = name,
            Description = description,
            Website = website,
            Country = country,
            CvFilePath = cvFilePath,
            GithubUrl = githubUrl,
            Status = ApplicationStatus.Pending
        });

        await _uow.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> ApproveAsync(string applicationId)
    {
        var app = await _uow.Repository<DeveloperApplication>().Query()
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == applicationId);

        if (app == null) return (false, "Application not found.");
        if (app.Status != ApplicationStatus.Pending) return (false, "Application is no longer pending.");

        app.Status = ApplicationStatus.Approved;
        _uow.Repository<DeveloperApplication>().Update(app);

        var slug = app.Name.ToLower().Replace(" ", "-");
        var (success, error) = await _devService.CreateOrUpdateProfileAsync(app.UserId, app.Name, slug, app.Description, app.Website, null, app.Country);
        if (!success) return (false, error);

        var (roleSuccess, roleError) = await _userService.ChangeRoleAsync(app.UserId, Role.DEVELOPER);
        if (!roleSuccess) return (false, roleError);

        await _uow.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> RejectAsync(string applicationId)
    {
        var app = await _uow.Repository<DeveloperApplication>().GetByIdAsync(applicationId);
        if (app == null) return (false, "Application not found.");

        _uow.Repository<DeveloperApplication>().Delete(app);
        await _uow.SaveChangesAsync();
        return (true, string.Empty);
    }
}
