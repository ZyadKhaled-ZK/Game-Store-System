namespace GameStore.PL.Pages.Admin
{
    public class ManageReviewsModel : PageModel
    {
        private readonly IReviewService _reviewService;

        public ManageReviewsModel(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        public List<Review> Reviews { get; set; } = new();
        public string Message { get; set; } = string.Empty;
        public bool IsError { get; set; }

        public async Task<IActionResult> OnGet()
        {
            if (TempData.TryGetValue("Message", out var msg)) Message = msg?.ToString() ?? "";
            if (TempData.TryGetValue("IsError", out var err)) IsError = err is bool b && b;
            Reviews = await _reviewService.GetAllWithDetailsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostDeleteAsync(string id)
        {
            await _reviewService.DeleteAsync(id);
            TempData["Message"] = "Review deleted.";
            TempData["IsError"] = false;
            return RedirectToPage();
        }
    }
}
