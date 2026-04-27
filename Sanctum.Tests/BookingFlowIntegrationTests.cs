using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Sanctum.Data;
using Sanctum.Models;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Sanctum.Tests;

// Logs in as a seeded user (cookie auth), books a slot, then confirms it
// shows up in GetMyBookings
public class BookingFlowIntegrationTests : IClassFixture<SanctumWebApplicationFactory>
{
    private readonly SanctumWebApplicationFactory _factory;

    public BookingFlowIntegrationTests(SanctumWebApplicationFactory factory)
    {
        _factory = factory;
        SeedUser("alice@x.com", "correct-horse");
        SeedUser("bob@x.com",   "correct-horse");
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

    // Drives the real /Home/Login endpoint so the resulting auth cookie ends up
    // in `client`'s cookie jar. Subsequent requests on the same client are
    // authenticated as `username`.
    private static async Task LoginAsync(HttpClient client, string username, string password)
    {
        var resp = await client.PostAsync("/Home/Login",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["username"] = username,
                ["password"] = password
            }));
        Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);
    }

    [Fact]
    public async Task Login_ThenBook_ThenSeeBookingInList()
    {
        // AllowAutoRedirect=false so we can assert the 302 from Login directly.
        // The default cookie container still carries the auth cookie forward.
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Log in. Expect a 302 redirect to /Booking with a Set-Cookie.
        var loginResp = await client.PostAsync("/Home/Login",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["username"] = "alice@x.com",
                ["password"] = "correct-horse"
            }));
        Assert.Equal(HttpStatusCode.Redirect, loginResp.StatusCode);

        // Book a slot. Expect JSON { success = true }.
        var bookResp = await client.PostAsync("/Booking/ConfirmBooking",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["building"] = "Library",
                ["room"]     = "101",
                ["date"]     = "2026-04-18",
                ["time"]     = "14:00-15:00"
            }));
        bookResp.EnsureSuccessStatusCode();
        var bookJson = await bookResp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(bookJson.GetProperty("success").GetBoolean());

        // Fetch the user's bookings. Expect the slot we just made to be there.
        var listResp = await client.GetAsync("/Booking/GetMyBookings");
        listResp.EnsureSuccessStatusCode();
        var listJson = await listResp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(listJson.GetProperty("success").GetBoolean());

        var bookings = listJson.GetProperty("bookings");
        Assert.Equal(1, bookings.GetArrayLength());
        Assert.Equal("Library", bookings[0].GetProperty("building").GetString());
        Assert.Equal("101",     bookings[0].GetProperty("room").GetString());
    }
    
    [Fact]
    public async Task ConfirmBooking_WithoutLogin_DoesNotCreateBooking()
    {
        // Fresh client, no auth cookie will be sent.
        // No login step is performed, simulating a curl/devtools request.
        var client = _factory.CreateClient();

        // POST a booking request to the endpoint.
        // Building/room/date are distinct from the other test's
        // ("Library|202" vs "Library|101") so the DB check below is unambiguous
        // even when tests run against the same factory's shared in-memory DB.
        var resp = await client.PostAsync("/Booking/ConfirmBooking",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["building"] = "Library",
                ["room"]     = "202",
                ["date"]     = "2026-04-19",
                ["time"]     = "10:00-11:00"
            }));

        // The endpoint always returns 200 + JSON envelope; the boundary is enforced
        // via { success = false }
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(json.GetProperty("success").GetBoolean());

        // Even if the API returned success=false, confirm
        // no row was inserted.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False(db.Bookings.Any(b => b.Description == "Library|202"));
    }
    
    // Verifies the cookie auth middleware actually clears state on sign-out.
    [Fact]
    public async Task Logout_InvalidatesAuthCookie()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginAsync(client, "alice@x.com", "correct-horse");

        // A quick check: while logged in, GetMyBookings is authenticated and returns success=true.
        var beforeResp = await client.GetAsync("/Booking/GetMyBookings");
        beforeResp.EnsureSuccessStatusCode();
        Assert.True((await beforeResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("success").GetBoolean());

        // Sign out. The controller responds with a 302 and a Set-Cookie that
        // expires the auth cookie. The HttpClient's cookie container applies
        // it automatically, so the next request goes out without auth.
        var logoutResp = await client.PostAsync("/Home/Logout", new FormUrlEncodedContent([]));
        Assert.Equal(HttpStatusCode.Redirect, logoutResp.StatusCode);

        // Same endpoint, same client — but now unauthenticated.
        // Falls into the "User not found" branch and returns success=false.
        var afterResp = await client.GetAsync("/Booking/GetMyBookings");
        afterResp.EnsureSuccessStatusCode();
        Assert.False((await afterResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task TwoUsers_BobCannotSeeOrCancelAlicesBooking()
    {
        // Two clients. AllowAutoRedirect=false so the LoginAsync helper
        // can assert the 302 directly instead of following it into a view that doesn't exist
        // in the test pipeline.
        var aliceClient = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var bobClient   = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        await LoginAsync(aliceClient, "alice@x.com", "correct-horse");
        await LoginAsync(bobClient,   "bob@x.com",   "correct-horse");

        // Alice books Library|303 at 14:00.
        var bookResp = await aliceClient.PostAsync("/Booking/ConfirmBooking",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["building"] = "Library",
                ["room"]     = "303",
                ["date"]     = "2026-04-20",
                ["time"]     = "14:00-15:00"
            }));
        bookResp.EnsureSuccessStatusCode();
        Assert.True((await bookResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("success").GetBoolean());

        // Need Alice's booking ID for the cancel attempt below — pull it from her own
        // GetMyBookings rather than reaching into the DB, so the test stays end-to-end.
        var aliceList = await (await aliceClient.GetAsync("/Booking/GetMyBookings"))
            .Content.ReadFromJsonAsync<JsonElement>();
        var aliceBookings = aliceList.GetProperty("bookings");
        var aliceBooking = aliceBookings.EnumerateArray()
            .Single(b => b.GetProperty("room").GetString() == "303");
        var aliceBookingId = aliceBooking.GetProperty("id").GetInt32();

        // Bob's own booking list must not contain Alice's room.
        var bobList = await (await bobClient.GetAsync("/Booking/GetMyBookings"))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(bobList.GetProperty("success").GetBoolean());
        Assert.DoesNotContain(bobList.GetProperty("bookings").EnumerateArray(),
            b => b.GetProperty("room").GetString() == "303");

        // Bob can see the slot is taken on the room calendar.
        // If this ever returns empty,
        // Bob's UI would let him try to double-book and only fail at submit.
        var slotsResp = await bobClient.GetAsync("/Booking/GetBookedSlots?building=Library&room=303&date=2026-04-20");
        slotsResp.EnsureSuccessStatusCode();
        var slots = (await slotsResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("bookedSlots").EnumerateArray()
            .Select(s => s.GetString()).ToList();
        Assert.Contains("14:00", slots);

        // Bob tries to cancel Alice's booking by ID. The CancelBooking query
        // filters by `b.UserID == user.Id`, so Bob's session won't match — expect
        // success=false and Alice's booking still present.
        var cancelResp = await bobClient.SendAsync(new HttpRequestMessage(HttpMethod.Delete,
            $"/Booking/CancelBooking?id={aliceBookingId}"));
        cancelResp.EnsureSuccessStatusCode();
        Assert.False((await cancelResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("success").GetBoolean());

        // Confirm via Alice's session that her booking survived Bob's attempt.
        var aliceListAfter = await (await aliceClient.GetAsync("/Booking/GetMyBookings"))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(aliceListAfter.GetProperty("bookings").EnumerateArray(),
            b => b.GetProperty("id").GetInt32() == aliceBookingId);
    }
}
