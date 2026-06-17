using Microsoft.AspNetCore.Mvc.Filters;

namespace GameStore.PL
{
    public class AdminOnlyFilter : IAsyncPageFilter
    {
        public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context)
        {
            return Task.CompletedTask;
        }

        public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
        {
            var role = context.HttpContext.Session.GetString("Role");
            if (role != "ADMIN")
            {
                context.Result = new RedirectToPageResult("/Auth/Login");
                return;
            }
            await next();
        }
    }
}
