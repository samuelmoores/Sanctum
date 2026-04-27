using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using Sanctum.Data;
using Sanctum.Models;

namespace Sanctum.SystemTests;

// First end-to-end test: drives the login form in a real Chromium
// browser against a real Kestrel-bound app instance.
//
// Load login -> fill form -> submit -> verify the next page is the booking dashboard
public class LoginFlowSystemTest : IClassFixture<PlaywrightWebApplicationFactory>, IAsyncLifetime
{
    private readonly PlaywrightWebApplicationFactory _factory;
    private IPlaywright _playwright = null!;
    private IBrowser _browser = null!;

    public LoginFlowSystemTest(PlaywrightWebApplicationFactory factory)
    {
        _factory = factory;
        SeedUser("alice@x.com", "correct-horse");
    }

    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            // Headless = false is useful when debugging locally; keep true for CI.
            Headless = true
        });
    }

    public async Task DisposeAsync()
    {
        await _browser.DisposeAsync();
        _playwright.Dispose();
    }

    private void SeedUser(string username, string password)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (db.Users.Any(u => u.Username == username)) return;

        db.Users.Add(new User
        {
            Email = username,
            Username = username,
            First = "F", Last = "L",
            Password = BCrypt.Net.BCrypt.HashPassword(password),
            Description = "", CSULBID = ""
        });
        db.SaveChanges();
    }

    [Fact]
    public async Task UserCanLogInThroughTheUi()
    {
        var context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = _factory.ServerAddress,
            IgnoreHTTPSErrors = true
        });
        var page = await context.NewPageAsync();

        // Land on the login page.
        await page.GotoAsync("/Home/Login");

        // Fill the form. Selectors target the `name` attributes from Login.cshtml
        // (input[name="Username"] and input[name="Password"]).
        await page.FillAsync("input[name='Username']", "alice@x.com");
        await page.FillAsync("input[name='Password']", "correct-horse");

        // Submit. Wait for navigation to settle so the assertion below
        // runs against the page we landed on, not the form mid-submit.
        await Task.WhenAll(
            page.WaitForURLAsync("**/Home/Booking", new PageWaitForURLOptions { Timeout = 10_000 }),
            page.ClickAsync("button.login-btn")
        );

        // Assert we landed on the booking page
        await Assertions.Expect(page.Locator("h1.booking-title"))
            .ToHaveTextAsync("CSULB Room Booking");
    }

    // Failure-path counterpart to the happy login test: a wrong password must
    // re-render the login page with the user-visible error alert. The integration
    // test for this verifies the controller returns a ViewResult
    [Fact]
    public async Task WrongPasswordShowsErrorAlertOnLoginPage()
    {
        var context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = _factory.ServerAddress,
            IgnoreHTTPSErrors = true
        });
        var page = await context.NewPageAsync();

        await page.GotoAsync("/Home/Login");

        await page.FillAsync("input[name='Username']", "alice@x.com");
        await page.FillAsync("input[name='Password']", "definitely-not-the-right-password");

        // No navigation expected — the controller re-renders the same view with
        // ViewBag.alert set. Just click; the assertion below auto-waits for the
        // alert element to appear in the DOM.
        await page.ClickAsync("button.login-btn");

        // The alert div is conditionally rendered (Login.cshtml:30) — if @ViewBag.alert
        // is null, the div doesn't exist at all.
        var alert = page.Locator("div.alert.alert-danger");
        await Assertions.Expect(alert).ToBeVisibleAsync();
        await Assertions.Expect(alert).ToHaveTextAsync("Username or Password is Invalid!");
    }

    // Full happy-path E2E: log in, drive the JS-heavy booking grid (building pin
    // -> room -> calendar date -> time slot -> confirm), then open the "My Bookings"
    // modal and assert the just-made booking shows up.
    [Fact]
    public async Task UserCanBookARoomAndSeeItInMyBookings()
    {
        var context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = _factory.ServerAddress,
            IgnoreHTTPSErrors = true
        });
        var page = await context.NewPageAsync();

        // The app uses native window.alert() to confirm a booking and to show
        // failures. Auto-accept any dialog so the click that triggers it doesn't
        // hang the test. MUST be registered before the click that opens the dialog.
        page.Dialog += async (_, dialog) => await dialog.AcceptAsync();

        // Log in
        await page.GotoAsync("/Home/Login");
        await page.FillAsync("input[name='Username']", "alice@x.com");
        await page.FillAsync("input[name='Password']", "correct-horse");
        await Task.WhenAll(
            page.WaitForURLAsync("**/Home/Booking", new PageWaitForURLOptions { Timeout = 10_000 }),
            page.ClickAsync("button.login-btn")
        );

        // Wait for site.js init() to finish loading the rooms cache.
        // The building-pin click handler reads from a `rooms` object populated by
        // a /Room/GetRooms fetch on page load.
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Pick a building (HC).
        // Building pins are rendered server-side in Booking.cshtml with data-building
        // attributes — clicking one triggers site.js to render the room list.
        await page.ClickAsync("button.building-pin[data-building='HC']");

        // Pick a room.
        // Room buttons are JS-rendered after /Room/GetRooms returns.
        var roomButton = page.Locator("button.room-btn", new PageLocatorOptions { HasTextString = "HC-101" }).First;
        await Assertions.Expect(roomButton).ToBeVisibleAsync();
        await roomButton.ClickAsync();

        // Pick a date.
        // After a building+room are selected, today's calendar cell is enabled.
        // The .today class is set in renderCalendar() when the cell matches new Date().
        await page.ClickAsync(".calendar-cell.today");

        // Pick a time slot.
        // For HC, time slots run 9 AM - 8 PM. Pick "9:00 AM - 10:00 AM"
        var timeSlot = page.Locator("button.time-slot-btn", new PageLocatorOptions { HasTextString = "9:00 AM - 10:00 AM" });
        await Assertions.Expect(timeSlot).ToBeVisibleAsync();
        await timeSlot.ClickAsync();

        // Confirm the booking
        // The button is disabled until all four selections are made; the assertion
        // both proves the JS state machine got there and auto-waits for the buttons to enable.
        var confirmBtn = page.Locator("#confirmBookingBtn");
        await Assertions.Expect(confirmBtn).ToBeEnabledAsync();
        await confirmBtn.ClickAsync();
        // The dialog handler registered above auto-accepts the success alert here.

        // Open the "My Bookings" modal
        // Profile dropdown must be opened first; the "View My Bookings" button
        // lives inside it and isn't visible/clickable until the dropdown shows.
        await page.ClickAsync("#profileMenuBtn");
        await page.ClickAsync("#viewBookingsBtn");

        // Assert the booking appears
        // The card is JS-rendered after /Booking/GetMyBookings returns. Locator
        // auto-waits for it to appear in the DOM.
        var bookingCard = page.Locator(".booking-history-card").First;
        await Assertions.Expect(bookingCard).ToBeVisibleAsync();
        await Assertions.Expect(bookingCard).ToContainTextAsync("HC");
        await Assertions.Expect(bookingCard).ToContainTextAsync("HC-101");
    }
}
