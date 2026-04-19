using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sanctum.Controllers;
using Sanctum.Data;
using Sanctum.Models;
using System.Security.Claims;

namespace Sanctum.Tests;

public class HomeControllerTests
{
    // In-memory DB per test so tests can't leak into each other.
    private static AppDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    // Minimal stub of the ASP.NET authentication service.
    // HomeController.Login calls HttpContext.SignInAsync(...) on success; that call
    // resolves IAuthenticationService from the request services. In a real request
    // the framework wires up a cookie-ased one. here we capture whether it was called.
    private class FakeAuthService : IAuthenticationService
    {
        public bool SignInCalled { get; private set; }
        public ClaimsPrincipal? SignedInPrincipal { get; private set; }

        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme)
            => Task.FromResult(AuthenticateResult.NoResult());
        public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
            => Task.CompletedTask;
        public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
            => Task.CompletedTask;
        public Task SignInAsync(HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties)
        {
            SignInCalled = true;
            SignedInPrincipal = principal;
            return Task.CompletedTask;
        }
        public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
            => Task.CompletedTask;
    }

    // Controller.View() resolves ITempDataDictionaryFactory from the request services.
    private class NullTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object?> LoadTempData(HttpContext context) => new Dictionary<string, object?>();
        public void SaveTempData(HttpContext context, IDictionary<string, object?> values) { }
    }

    private class NullTempDataFactory : ITempDataDictionaryFactory
    {
        public ITempDataDictionary GetTempData(HttpContext context)
            => new TempDataDictionary(context, new NullTempDataProvider());
    }

    // ControllerBase.RedirectToAction() touches Controller.Url, which resolves IUrlHelperFactory.
    private class NullUrlHelperFactory : IUrlHelperFactory
    {
        public IUrlHelper GetUrlHelper(ActionContext context) => new NullUrlHelper(context);
    }

    private class NullUrlHelper(ActionContext ctx) : IUrlHelper
    {
        public ActionContext ActionContext { get; } = ctx;
        public string? Action(UrlActionContext actionContext) => null;
        public string? Content(string? contentPath) => contentPath;
        public bool IsLocalUrl(string? url) => false;
        public string? Link(string? routeName, object? values) => null;
        public string? RouteUrl(UrlRouteContext routeContext) => null;
    }

    // Builds a HomeController wired to our in-memory DB, the fake auth service,
    // and the minimum stub services the framework needs to let View() and RedirectToAction() run.
    private static (HomeController controller, FakeAuthService auth) BuildController(AppDbContext db)
    {
        var auth = new FakeAuthService();
        var services = new ServiceCollection();
        services.AddSingleton<IAuthenticationService>(auth);
        services.AddSingleton<ITempDataDictionaryFactory, NullTempDataFactory>();
        services.AddSingleton<IUrlHelperFactory, NullUrlHelperFactory>();

        var httpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };

        var controller = new HomeController(db)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
        return (controller, auth);
    }

    // Security test: an unknown username must not produce a sign-in.
    // The controller should fall back to re-rendering the login view instead of redirecting.
    [Fact]
    public async Task Login_UnknownUsername_DoesNotSignInAndReturnsView()
    {
        await using var db = NewDb();
        var (controller, auth) = BuildController(db);

        var result = await controller.Login("ghost@x.com", "whatever");

        Assert.IsType<ViewResult>(result);
        Assert.False(auth.SignInCalled);
    }

    // Security test: correct username + wrong password must NOT produce a sign-in.
    // If this regresses, anyone could log in with just a valid username.
    [Fact]
    public async Task Login_WrongPassword_DoesNotSignInAndReturnsView()
    {
        await using var db = NewDb();
        db.Users.Add(new User
        {
            Username = "alice@x.com",
            First = "A", Last = "A",
            Password = "correct-horse",
            Description = "", CSULBID = ""
        });
        db.SaveChanges();

        var (controller, auth) = BuildController(db);

        var result = await controller.Login("alice@x.com", "wrong");

        Assert.IsType<ViewResult>(result);
        Assert.False(auth.SignInCalled);
    }

    // Happy-path counterpart: correct credentials should sign the user in and
    // redirect to the Booking page. Also confirms the claim carries the right username.
    [Fact]
    public async Task Login_ValidCredentials_SignsInAndRedirectsToBooking()
    {
        await using var db = NewDb();
        db.Users.Add(new User
        {
            Username = "alice@x.com",
            First = "A", Last = "A",
            Password = "correct-horse",
            Description = "", CSULBID = ""
        });
        db.SaveChanges();

        var (controller, auth) = BuildController(db);

        var result = await controller.Login("alice@x.com", "correct-horse");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Booking", redirect.ActionName);
        Assert.True(auth.SignInCalled);
        Assert.Equal("alice@x.com", auth.SignedInPrincipal?.FindFirst(ClaimTypes.Name)?.Value);
    }
}
