using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sanctum.Controllers;
using Sanctum.Data;
using Sanctum.Models;

namespace Sanctum.Tests;

public class UserControllerTests
{
    private static AppDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    // Stub so the controller's RedirectToAction(...) can resolve Controller.Url.
    // UserController never calls View(), so no need a TempData stub here.
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

    private static UserController BuildController(AppDbContext db)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IUrlHelperFactory, NullUrlHelperFactory>();

        var httpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };

        return new UserController(db)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
    }

    private static User NewUser(string username, string password = "p") => new()
    {
        Username = username,
        First = "F", Last = "L",
        Password = password,
        Description = "", CSULBID = ""
    };

    // Data-integrity test: a user must not be able to rename their email
    // to one already owned by a different user (would break unique-username assumption).
    [Fact]
    public async Task UpdateEmail_ThrowsWhenNewEmailAlreadyTakenByAnotherUser()
    {
        using var db = NewDb();

        // Arrange: two users, each with their own email.
        var alice = NewUser("alice@x.com");
        var bob   = NewUser("bob@x.com");
        db.Users.AddRange(alice, bob);
        db.SaveChanges();

        var controller = BuildController(db);

        // Act: Alice tries to change her email to Bob's.
        // Assert: the controller throws instead of overwriting.
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => controller.UpdateEmail(alice.Id, "bob@x.com"));

        // Assert: neither user's email changed in the DB.
        Assert.Equal("alice@x.com", db.Users.Find(alice.Id)!.Username);
        Assert.Equal("bob@x.com",   db.Users.Find(bob.Id)!.Username);
    }

    // Guardrail: updating an email for a user ID that doesn't exist should throw,
    // an error, not nothing.
    [Fact]
    public async Task UpdateEmail_ThrowsWhenUserIdNotFound()
    {
        // Arrange: empty DB — user ID 999 doesn't exist.
        using var db = NewDb();
        var controller = BuildController(db);

        // Act + Assert: calling with an unknown ID throws KeyNotFoundException.
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => controller.UpdateEmail(999, "anything@x.com"));
    }

    // Happy path: changing to a genuinely unused email persists the change
    // and the action redirects (back to the home index).
    [Fact]
    public async Task UpdateEmail_Succeeds_WhenNewEmailIsUnique()
    {
        using var db = NewDb();

        // Arrange: one user in the DB.
        var alice = NewUser("alice@x.com");
        db.Users.Add(alice);
        db.SaveChanges();

        var controller = BuildController(db);

        // Act: Alice changes her email to something nobody else has.
        var result = await controller.UpdateEmail(alice.Id, "alice-new@x.com");

        // Assert: action redirects, and the DB now has the new email.
        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("alice-new@x.com", db.Users.Find(alice.Id)!.Username);
    }

    // Updating a user's own email to the *same* email should succeed —
    // the duplicate check explicitly excludes the current user (`u.Id != id`).
    [Fact]
    public async Task UpdateEmail_AllowsUserToKeepTheirOwnEmail()
    {
        using var db = NewDb();

        // Arrange: Alice exists.
        var alice = NewUser("alice@x.com");
        db.Users.Add(alice);
        db.SaveChanges();

        var controller = BuildController(db);

        // Act: Alice submits the form with her existing email unchanged.
        var result = await controller.UpdateEmail(alice.Id, "alice@x.com");

        // Assert: redirect succeeds and Alice's email is unchanged.
        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("alice@x.com", db.Users.Find(alice.Id)!.Username);
    }
}
