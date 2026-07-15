using FluentAssertions;
using GameStore.PL.Filters;
using GameStore.Tests.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace GameStore.Tests.Filters;

public class AdminOnlyFilterTests
{
    private readonly Mock<IUserService> _userServiceMock;

    public AdminOnlyFilterTests()
    {
        _userServiceMock = new Mock<IUserService>();
    }

    private static ActionContext CreateActionContext(HttpContext httpContext)
    {
        var ctor = typeof(ActionContext).GetConstructors().First(c =>
        {
            var p = c.GetParameters();
            return p.Length == 3 &&
                   p[0].ParameterType == typeof(HttpContext) &&
                   p[2].ParameterType.Name == "ActionDescriptor";
        });
        var descriptor = Activator.CreateInstance(
            ctor.GetParameters()[2].ParameterType)!;
        return (ActionContext)ctor.Invoke(new object[] { httpContext, new RouteData(), descriptor });
    }

    private static AuthorizationFilterContext CreateContext(
        bool isAuthenticated = false,
        string? roleClaim = null,
        string? sessionId = null,
        string? sessionRole = null)
    {
        var httpContext = new DefaultHttpContext();

        if (isAuthenticated)
        {
            var claims = new List<Claim>();
            if (roleClaim != null)
                claims.Add(new Claim("Role", roleClaim));
            var identity = new ClaimsIdentity(claims, "TestAuth");
            httpContext.User = new ClaimsPrincipal(identity);
        }

        var session = new MockHttpSession();
        if (sessionId != null)
            session.SetString("UserId", sessionId);
        if (sessionRole != null)
            session.SetString("Role", sessionRole);
        httpContext.Session = session;

        var actionContext = CreateActionContext(httpContext);
        return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
    }

    [Fact]
    public async Task AdminUser_ThroughCookie_Authorized()
    {
        var context = CreateContext(isAuthenticated: true, roleClaim: "ADMIN");
        var filter = new AdminOnlyFilter(_userServiceMock.Object);

        await filter.OnAuthorizationAsync(context);

        context.Result.Should().BeNull();
    }

    [Fact]
    public async Task CustomerUser_ThroughCookie_RedirectsToHome()
    {
        var context = CreateContext(isAuthenticated: true, roleClaim: "CUSTOMER");
        var filter = new AdminOnlyFilter(_userServiceMock.Object);

        await filter.OnAuthorizationAsync(context);

        context.Result.Should().BeOfType<RedirectToActionResult>();
        var redirect = (RedirectToActionResult)context.Result!;
        redirect.ControllerName.Should().Be("Home");
    }

    [Fact]
    public async Task UnauthenticatedUser_WithAdminSession_Authorized()
    {
        var context = CreateContext(sessionId: "u1", sessionRole: "ADMIN");
        _userServiceMock.Setup(s => s.GetByIdAsync("u1"))
            .ReturnsAsync(new User { Id = "u1", Role = Role.ADMIN });
        var filter = new AdminOnlyFilter(_userServiceMock.Object);

        await filter.OnAuthorizationAsync(context);

        context.Result.Should().BeNull();
    }

    [Fact]
    public async Task UnauthenticatedUser_WithNonAdminSession_RedirectsToHome()
    {
        var context = CreateContext(sessionId: "u1", sessionRole: "CUSTOMER");
        _userServiceMock.Setup(s => s.GetByIdAsync("u1"))
            .ReturnsAsync(new User { Id = "u1", Role = Role.CUSTOMER });
        var filter = new AdminOnlyFilter(_userServiceMock.Object);

        await filter.OnAuthorizationAsync(context);

        context.Result.Should().BeOfType<RedirectToActionResult>();
    }

    [Fact]
    public async Task NoSessionNoAuth_RedirectsToLogin()
    {
        var context = CreateContext();
        var filter = new AdminOnlyFilter(_userServiceMock.Object);

        await filter.OnAuthorizationAsync(context);

        context.Result.Should().BeOfType<RedirectToActionResult>();
        var redirect = (RedirectToActionResult)context.Result!;
        redirect.ControllerName.Should().Be("Auth");
        redirect.ActionName.Should().Be("Login");
    }

    [Fact]
    public async Task SessionRoleMismatch_UpdatesSessionRole()
    {
        var context = CreateContext(sessionId: "u1", sessionRole: "CUSTOMER");
        _userServiceMock.Setup(s => s.GetByIdAsync("u1"))
            .ReturnsAsync(new User { Id = "u1", Role = Role.ADMIN });
        var filter = new AdminOnlyFilter(_userServiceMock.Object);

        await filter.OnAuthorizationAsync(context);

        context.Result.Should().BeNull();
        context.HttpContext.Session.GetString("Role").Should().Be("ADMIN");
    }

    [Fact]
    public async Task DeveloperUser_ThroughCookie_RedirectsToHome()
    {
        var context = CreateContext(isAuthenticated: true, roleClaim: "DEVELOPER");
        var filter = new AdminOnlyFilter(_userServiceMock.Object);

        await filter.OnAuthorizationAsync(context);

        context.Result.Should().BeOfType<RedirectToActionResult>();
    }
}
