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

        public async Task<IActionResult> OnGet()
        {
            Reviews = await _reviewService.GetAllWithDetailsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostDeleteAsync(string id)
        {
            await _reviewService.DeleteAsync(id);
            return RedirectToPage();
        }
    }
}
