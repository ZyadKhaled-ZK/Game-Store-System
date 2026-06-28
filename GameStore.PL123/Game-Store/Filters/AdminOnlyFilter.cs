using Microsoft.AspNetCore.Mvc.Filters;

namespace GameStore.PL.Filters;

public class AdminOnlyFilter : IAsyncAuthorizationFilter
{
    private readonly IUserService _userService;

    public AdminOnlyFilter(IUserService userService)
    {
        _userService = userService;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var userId = context.HttpContext.Session.GetString("UserId");
        if (!string.IsNullOrEmpty(userId))
        {
            var user = await _userService.GetByIdAsync(userId);
            if (user != null)
            {
                var currentRole = user.Role.ToString();
                var sessionRole = context.HttpContext.Session.GetString("Role");
                if (sessionRole != currentRole)
                    context.HttpContext.Session.SetString("Role", currentRole);
            }
        }

        if (!string.IsNullOrEmpty(context.HttpContext.Session.GetString("UserId")))
        {
            var role = context.HttpContext.Session.GetString("Role");
            if (role != Role.ADMIN.ToString())
            {
                context.Result = new RedirectToActionResult("Index", "Home", new { area = "" });
            }
        }
        else
        {
            context.Result = new RedirectToActionResult("Login", "Auth", new { area = "" });
        }
    }
}
