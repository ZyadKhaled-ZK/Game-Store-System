using GameStore.PL.Models.Home;

namespace GameStore.PL.Controllers;

public class ReviewsController : Controller
{
    private readonly IReviewService _reviewService;

    public ReviewsController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddReview([FromBody] ReviewRequest request)
    {
        if (!ModelState.IsValid)
            return Json(new { success = false, message = "Invalid input." });

        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId))
            return Json(new { success = false, message = "Please login first." });

        var (success, error) = await _reviewService.CreateAsync(userId, request.GameId, request.Rating, request.Comment);
        return Json(new { success, message = error });
    }
}