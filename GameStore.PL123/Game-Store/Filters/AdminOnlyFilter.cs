using Microsoft.AspNetCore.Mvc.Filters;

namespace GameStore.PL.Filters;

public class AdminOnlyFilter : IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var role = context.HttpContext.Session.GetString("Role");
        if (role != Role.ADMIN.ToString())
        {
            context.Result = new RedirectToActionResult("Login", "Auth", null);
        }
    }
}
