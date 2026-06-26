using System.ComponentModel.DataAnnotations;

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
        public string Message { get; set; } = string.Empty;
        public bool IsError { get; set; }

        public async Task<IActionResult> OnGet()
        {
            if (TempData.TryGetValue("Message", out var msg)) Message = msg?.ToString() ?? "";
            if (TempData.TryGetValue("IsError", out var err)) IsError = err is bool b && b;
            Users = await _userService.GetAllAsync();
            return Page();
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostChangeRoleAsync([Required] string id, [Range(0, 2)] int role)
        {
            if (!ModelState.IsValid) return RedirectToPage();
            var currentUserId = HttpContext.Session.GetString("UserId");
            var (success, error) = await _userService.ChangeRoleAsync(id, (Role)role, currentUserId);
            TempData["Message"] = success ? "Role updated." : error;
            TempData["IsError"] = !success;
            return RedirectToPage();
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostDeleteAsync([Required] string id)
        {
            if (!ModelState.IsValid) return RedirectToPage();
            var currentUserId = HttpContext.Session.GetString("UserId");
            var (success, error) = await _userService.DeleteAsync(id, currentUserId);
            TempData["Message"] = success ? "User terminated." : error;
            TempData["IsError"] = !success;
            return RedirectToPage();
        }
    }
}
