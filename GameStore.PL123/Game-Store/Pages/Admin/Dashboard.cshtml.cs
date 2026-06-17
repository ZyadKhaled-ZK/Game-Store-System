namespace GameStore.PL.Pages.Admin
{
    public class DashboardModel : PageModel
    {
        private readonly IUserService _userService;
        private readonly IGameService _gameService;
        private readonly IOrderService _orderService;

        public DashboardModel(IUserService userService, IGameService gameService, IOrderService orderService)
        {
            _userService = userService;
            _gameService = gameService;
            _orderService = orderService;
        }

        public int TotalUsers { get; set; }
        public int TotalGames { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }

        public async Task<IActionResult> OnGet()
        {
            TotalUsers = await _userService.GetTotalUsersAsync();
            TotalGames = await _gameService.GetTotalGamesAsync();
            TotalOrders = await _orderService.GetTotalOrdersAsync();
            TotalRevenue = await _orderService.GetTotalRevenueAsync();

            return Page();
        }
    }
}
