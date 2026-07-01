using GameStore.DAL.Entities;
using GameStore.DAL.Enum;
using GameStore.DAL.Repo;
using GameStore.PL.Services;
using Microsoft.EntityFrameworkCore;

namespace GameStore.PL.Controllers;

public class NotificationsController : Controller
{
    private readonly IUnitOfWork _uow;
    private readonly ISaleService _saleService;
    private readonly IDeveloperApplicationService _devAppService;

    public NotificationsController(IUnitOfWork uow, ISaleService saleService, IDeveloperApplicationService devAppService)
    {
        _uow = uow;
        _saleService = saleService;
        _devAppService = devAppService;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId))
            return Json(new { unread = 0, adminPending = 0 });
        var role = HttpContext.Session.GetString("Role");
        var isAdmin = role == "ADMIN";

        var unread = await _uow.Repository<UserNotification>().Query()
            .CountAsync(n => (n.UserId == userId || (isAdmin && n.UserId == null)) && n.ReadAt == null);

        var adminPending = 0;
        if (isAdmin)
        {
            var pendingSales = await _uow.Repository<Sale>().Query()
                .CountAsync(s => s.Status == SaleStatus.Pending);
            var pendingApps = await _uow.Repository<DeveloperApplication>().Query()
                .CountAsync(a => a.Status == ApplicationStatus.Pending);
            adminPending = pendingSales + pendingApps;
        }

        return Json(new { unread, adminPending });
    }

    [HttpGet]
    public async Task<IActionResult> GetRecent()
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId))
            return Json(new { notifications = new List<object>() });
        var role = HttpContext.Session.GetString("Role");
        var isAdmin = role == "ADMIN";

        var items = await _uow.Repository<UserNotification>().Query()
            .Where(n => n.UserId == userId || (isAdmin && n.UserId == null))
            .OrderByDescending(n => n.CreatedAt)
            .Take(5)
            .Select(n => new
            {
                id = n.Id,
                title = n.Title,
                message = n.Message,
                type = n.Type,
                category = n.Category,
                referenceUrl = n.ReferenceUrl,
                createdAt = n.CreatedAt.ToString("MMM dd"),
                isRead = n.ReadAt != null
            })
            .ToListAsync();

        return Json(new { notifications = items });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsRead(string id)
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var role = HttpContext.Session.GetString("Role");
        var isAdmin = role == "ADMIN";
        var notif = await _uow.Repository<UserNotification>().GetByIdAsync(id);
        if (notif == null) return NotFound();
        if (notif.UserId != null && notif.UserId != userId) return NotFound();
        if (notif.UserId == null && !isAdmin) return NotFound();

        _uow.Repository<UserNotification>().Delete(notif);
        await _uow.SaveChangesAsync();

        return Ok();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId)) return Unauthorized();
        var role = HttpContext.Session.GetString("Role");
        var isAdmin = role == "ADMIN";

        var unread = await _uow.Repository<UserNotification>().Query()
            .Where(n => (n.UserId == userId || (isAdmin && n.UserId == null)) && n.ReadAt == null)
            .ToListAsync();

        foreach (var n in unread)
            _uow.Repository<UserNotification>().Delete(n);

        var unreadMessages = await _uow.Repository<Message>().Query()
            .Where(m => m.ReceiverId == userId && m.ReadAt == null)
            .ToListAsync();

        foreach (var m in unreadMessages)
            m.ReadAt = DateTime.UtcNow;

        if (unread.Count > 0 || unreadMessages.Count > 0)
            await _uow.SaveChangesAsync();

        return Ok();
    }

    [HttpGet]
    public async Task<IActionResult> GetAdminNotificationData()
    {
        var userId = HttpContext.Session.GetString("UserId");
        var role = HttpContext.Session.GetString("Role");
        if (string.IsNullOrEmpty(userId) || role != "ADMIN")
            return Json(new { unread = 0, pendingSales = 0, pendingApplications = 0, total = 0, notifications = new List<object>() });

        var unread = await _uow.Repository<UserNotification>().Query()
            .Where(n => (n.UserId == userId || n.UserId == null) && n.ReadAt == null)
            .CountAsync();

        var pendingSales = await _uow.Repository<Sale>().Query()
            .CountAsync(s => s.Status == SaleStatus.Pending);
        var pendingApplications = await _uow.Repository<DeveloperApplication>().Query()
            .CountAsync(a => a.Status == ApplicationStatus.Pending);

        var recentNotifs = await _uow.Repository<UserNotification>().Query()
            .Where(n => n.UserId == userId || n.UserId == null)
            .OrderByDescending(n => n.CreatedAt)
            .Take(5)
            .Select(n => new
            {
                id = n.Id,
                title = n.Title,
                message = n.Message,
                type = n.Type,
                category = n.Category,
                referenceUrl = n.ReferenceUrl,
                createdAt = n.CreatedAt.ToString("MMM dd"),
                isRead = n.ReadAt != null
            })
            .ToListAsync();

        return Json(new
        {
            unread,
            pendingSales,
            pendingApplications,
            total = unread + pendingSales + pendingApplications,
            notifications = recentNotifs
        });
    }
}
