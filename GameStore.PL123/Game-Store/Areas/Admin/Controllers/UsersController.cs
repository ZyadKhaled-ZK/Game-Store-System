using System.ComponentModel.DataAnnotations;
using GameStore.PL.Models.Admin;

namespace GameStore.PL.Areas.Admin.Controllers;

[Area("Admin")]
[ServiceFilter(typeof(AdminOnlyFilter))]
public class UsersController : Controller
{
    private readonly IUserService _userService;
    private readonly IOrderService _orderService;
    private readonly ILibraryService _libraryService;
    private readonly IReviewService _reviewService;

    public UsersController(IUserService userService, IOrderService orderService,
        ILibraryService libraryService, IReviewService reviewService)
    {
        _userService = userService;
        _orderService = orderService;
        _libraryService = libraryService;
        _reviewService = reviewService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var model = new ManageUsersViewModel();
        if (TempData.TryGetValue("Message", out var msg)) model.Message = msg?.ToString() ?? "";
        if (TempData.TryGetValue("IsError", out var err)) model.IsError = err is bool b && b;
        model.Users = await _userService.GetAllAsync();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeRole([Required] string id, [Range(0, 2)] int role)
    {
        if (!ModelState.IsValid) return RedirectToAction("Index");
        var currentUserId = HttpContext.Session.GetString("UserId");
        var (success, error) = await _userService.ChangeRoleAsync(id, (Role)role, currentUserId);
        TempData["Message"] = success ? "Role updated." : error;
        TempData["IsError"] = !success;
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete([Required] string id)
    {
        if (!ModelState.IsValid) return RedirectToAction("Index");
        var currentUserId = HttpContext.Session.GetString("UserId");
        var (success, error) = await _userService.DeleteAsync(id, currentUserId);
        TempData["Message"] = success ? "User terminated." : error;
        TempData["IsError"] = !success;
        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> Details(string id)
    {
        if (string.IsNullOrEmpty(id))
            return RedirectToAction("Index");

        var user = await _userService.GetByIdAsync(id);
        if (user == null)
            return RedirectToAction("Index");

        var model = new UserDetailsViewModel
        {
            User = user,
            Orders = await _orderService.GetOrdersByUserAsync(id),
            LibraryGames = await _libraryService.GetLibraryGamesAsync(id),
            Reviews = await _reviewService.GetByUserAsync(id)
        };

        return View(model);
    }
}
