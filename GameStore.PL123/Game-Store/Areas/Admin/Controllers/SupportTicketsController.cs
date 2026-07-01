using GameStore.PL.Filters;

namespace GameStore.PL.Areas.Admin.Controllers;

[Area("Admin")]
[ServiceFilter(typeof(AdminOnlyFilter))]
public class SupportTicketsController : Controller
{
    private readonly ISupportTicketService _ticketService;
    private readonly INotificationService _notificationService;
    private readonly IUnitOfWork _uow;

    public SupportTicketsController(ISupportTicketService ticketService, INotificationService notificationService, IUnitOfWork uow)
    {
        _ticketService = ticketService;
        _notificationService = notificationService;
        _uow = uow;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1)
    {
        const int pageSize = 20;
        var tickets = await _ticketService.GetAllAsync(page, pageSize);
        var total = await _ticketService.GetCountAsync();

        if (TempData.TryGetValue("Message", out var msg)) ViewData["Message"] = msg;
        if (TempData.TryGetValue("IsError", out var err)) ViewData["IsError"] = err is bool b && b;

        ViewData["Page"] = page;
        ViewData["TotalPages"] = (int)Math.Ceiling(total / (double)pageSize);

        return View(tickets);
    }

    [HttpGet]
    public async Task<IActionResult> Details(string id)
    {
        if (string.IsNullOrEmpty(id))
            return RedirectToAction("Index");

        var ticket = await _ticketService.GetByIdAsync(id);
        if (ticket == null)
        {
            TempData["Message"] = "Ticket not found.";
            TempData["IsError"] = true;
            return RedirectToAction("Index");
        }

        return View(ticket);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reply(string id, string message)
    {
        var adminId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrWhiteSpace(message))
        {
            TempData["Message"] = "Message is required.";
            TempData["IsError"] = true;
            return RedirectToAction("Details", new { id });
        }

        await _ticketService.AddReplyAsync(id, adminId, message.Trim());

        var ticket = await _ticketService.GetByIdAsync(id);
        if (ticket?.UserId != null)
        {
            await _notificationService.SendToUserAsync(ticket.UserId,
                "Support Reply",
                $"Your ticket \"{ticket.Subject}\" has received a reply.",
                "info",
                "support",
                referenceUrl: "/Support/Details/" + id);
        }

        TempData["Message"] = "Reply posted.";
        return RedirectToAction("Details", new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(string id, TicketStatus status)
    {
        await _ticketService.UpdateStatusAsync(id, status);

        var ticket = await _ticketService.GetByIdAsync(id);
        if (ticket?.UserId != null)
        {
            var statusLabel = status switch
            {
                TicketStatus.InProgress => "In Progress",
                TicketStatus.Resolved => "Resolved",
                TicketStatus.Closed => "Closed",
                _ => "Open"
            };

            await _notificationService.SendToUserAsync(ticket.UserId,
                "Ticket Status Updated",
                $"Your ticket \"{ticket.Subject}\" is now {statusLabel}.",
                "info",
                "support",
                referenceUrl: "/Support/Details/" + id);
        }

        TempData["Message"] = $"Ticket status updated to {status}.";
        return RedirectToAction("Details", new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        if (string.IsNullOrEmpty(id)) return RedirectToAction("Index");

        var repo = _uow.Repository<SupportTicket>();
        var ticket = await repo.GetByIdAsync(id);

        if (ticket == null)
        {
            TempData["Message"] = "Ticket not found.";
            TempData["IsError"] = true;
            return RedirectToAction("Index");
        }

        repo.Delete(ticket);
        await _uow.SaveChangesAsync();

        TempData["Message"] = "Ticket deleted.";
        return RedirectToAction("Index");
    }
}
