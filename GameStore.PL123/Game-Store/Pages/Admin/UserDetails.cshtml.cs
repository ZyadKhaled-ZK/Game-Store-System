namespace GameStore.PL.Pages.Admin
{
    public class UserDetailsModel : PageModel
    {
        private readonly IUserService _userService;
        private readonly IOrderService _orderService;
        private readonly ILibraryService _libraryService;
        private readonly IReviewService _reviewService;

        public UserDetailsModel(IUserService userService, IOrderService orderService,
            ILibraryService libraryService, IReviewService reviewService)
        {
            _userService = userService;
            _orderService = orderService;
            _libraryService = libraryService;
            _reviewService = reviewService;
        }

        public User User { get; set; } = null!;
        public List<Order> Orders { get; set; } = new();
        public List<LibraryGame> LibraryGames { get; set; } = new();
        public List<Review> Reviews { get; set; } = new();

        public async Task<IActionResult> OnGet(string id)
        {
            if (string.IsNullOrEmpty(id))
                return RedirectToPage("/Admin/ManageUsers");

            var user = await _userService.GetByIdAsync(id);
            if (user == null)
                return RedirectToPage("/Admin/ManageUsers");

            User = user;
            Orders = await _orderService.GetOrdersByUserAsync(id);
            LibraryGames = await _libraryService.GetLibraryGamesAsync(id);
            Reviews = await _reviewService.GetByUserAsync(id);

            return Page();
        }
    }
}
