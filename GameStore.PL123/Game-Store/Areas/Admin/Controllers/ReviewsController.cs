using GameStore.PL.Models.Admin;

namespace GameStore.PL.Areas.Admin.Controllers;

[Area("Admin")]
[ServiceFilter(typeof(AdminOnlyFilter))]
public class ReviewsController : Controller
{
    private readonly IReviewService _reviewService;

    public ReviewsController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? page)
    {
        var model = new ManageReviewsViewModel();
        if (TempData.TryGetValue("Message", out var msg)) model.Message = msg?.ToString() ?? "";
        if (TempData.TryGetValue("IsError", out var err)) model.IsError = err is bool b && b;
        var paged = await _reviewService.GetAllPagedAsync(page ?? 1, 50);
        model.Reviews = paged.Items;
        model.CurrentPage = paged.Page;
        model.TotalPages = paged.TotalPages;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        await _reviewService.DeleteAsync(id);
        TempData["Message"] = "Review deleted.";
        TempData["IsError"] = false;
        return RedirectToAction("Index");
    }
}
