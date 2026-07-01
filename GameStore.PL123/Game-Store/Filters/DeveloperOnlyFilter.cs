using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GameStore.PL.Filters;

public class DeveloperOnlyFilter : IAsyncAuthorizationFilter
{
    private readonly IUserService _userService;

    public DeveloperOnlyFilter(IUserService userService)
    {
        _userService = userService;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // Check auth cookie first
        var user = context.HttpContext.User;
        if (user.Identity?.IsAuthenticated == true)
        {
            var roleClaim = user.FindFirstValue("Role");
            if (roleClaim == Role.DEVELOPER.ToString())
                return;

            context.Result = new RedirectToActionResult("Index", "BecomeDeveloper", new { area = "" });
            return;
        }

        // Fallback: session auth (users logged in before cookie auth was added)
        var userId = context.HttpContext.Session.GetString("UserId");
        if (!string.IsNullOrEmpty(userId))
        {
            var dbUser = await _userService.GetByIdAsync(userId);
            if (dbUser != null)
            {
                var currentRole = dbUser.Role.ToString();
                var sessionRole = context.HttpContext.Session.GetString("Role");
                if (sessionRole != currentRole)
                    context.HttpContext.Session.SetString("Role", currentRole);
            }

            var role = context.HttpContext.Session.GetString("Role");
            if (role == Role.DEVELOPER.ToString())
                return;

            context.Result = new RedirectToActionResult("Index", "BecomeDeveloper", new { area = "" });
            return;
        }

        context.Result = new RedirectToActionResult("Login", "Auth", new { area = "" });
    }
}
