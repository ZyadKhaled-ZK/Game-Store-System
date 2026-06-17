namespace GameStore.PL.Pages.Admin
{
    public class ManageUsersModel : PageModel
    {
        private readonly IUserService _userService;

        public ManageUsersModel(IUserService userService)
        {
            _userService = userService;
        }

        public List<User> Users { get; set; } = new();

        public async Task<IActionResult> OnGet()
        {
            Users = await _userService.GetAllAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostChangeRoleAsync(string id, int role)
        {
            var currentUserId = HttpContext.Session.GetString("UserId");
            await _userService.ChangeRoleAsync(id, (Role)role, currentUserId);
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(string id)
        {
            var currentUserId = HttpContext.Session.GetString("UserId");
            await _userService.DeleteAsync(id, currentUserId);
            return RedirectToPage();
        }
    }
}
