using GameStore.BLL.Services;

namespace GameStore.PL.Controllers;

public class SupportController : Controller
{
    private readonly ISupportTicketService _ticketService;
    private readonly INotificationService _notificationService;

    public SupportController(ISupportTicketService ticketService, INotificationService notificationService)
    {
        _ticketService = ticketService;
        _notificationService = notificationService;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(string subject, string message, string? email)
    {
        if (string.IsNullOrWhiteSpace(subject))
            ModelState.AddModelError("subject", "Subject is required.");
        if (string.IsNullOrWhiteSpace(message))
            ModelState.AddModelError("message", "Message is required.");
        if (message?.Length > 2000)
            ModelState.AddModelError("message", "Message must be under 2000 characters.");

        if (!ModelState.IsValid)
            return View();

        var userId = HttpContext.Session.GetString("UserId");

        await _ticketService.CreateAsync(userId, email, subject.Trim(), message.Trim());

        await _notificationService.SendToAdminsAsync(
            "New Support Ticket",
            $"Subject: {subject.Trim()}",
            "warning",
            "support",
            referenceUrl: "/Admin/SupportTickets"
        );

        TempData["TicketSuccess"] = "Your ticket has been submitted. We'll get back to you soon!";
        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> MyTickets(int page = 1)
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Auth");

        const int pageSize = 10;
        var tickets = await _ticketService.GetUserTicketsAsync(userId, page, pageSize);
        var total = await _ticketService.GetUserTicketCountAsync(userId);

        ViewData["Page"] = page;
        ViewData["TotalPages"] = (int)Math.Ceiling(total / (double)pageSize);

        return View(tickets);
    }

    [HttpGet]
    public async Task<IActionResult> Details(string id)
    {
        if (string.IsNullOrEmpty(id))
            return RedirectToAction("Index", "Home");

        var ticket = await _ticketService.GetByIdAsync(id);
        if (ticket == null)
            return RedirectToAction("Index", "Home");

        var userId = HttpContext.Session.GetString("UserId");
        if (!string.IsNullOrEmpty(userId) && ticket.UserId != userId)
            return RedirectToAction("Index", "Home");

        return View(ticket);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reply(string id, string message)
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId))
            return Json(new { success = false, message = "Please login first." });

        if (string.IsNullOrWhiteSpace(message))
            return Json(new { success = false, message = "Message is required." });

        var isOwner = await _ticketService.IsOwnerAsync(id, userId);
        if (!isOwner)
            return Json(new { success = false, message = "Not your ticket." });

        var reply = await _ticketService.AddReplyAsync(id, userId, message);
        return Json(new { success = true, id = reply.Id, createdAt = reply.CreatedAt.ToString("MMM dd, yyyy HH:mm") });
    }
}
