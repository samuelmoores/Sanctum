using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sanctum.Controllers;
using Sanctum.Data;
using Sanctum.Models;
using System.Security.Claims;

namespace Sanctum.Tests;

public class BookingControllerTests
{
    // Creates an in-memory database for each test so tests don't share state.
    private static AppDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    // Builds a BookingController with a fake logged-in user.
    // In real requests, ASP.NET fills HttpContext.User from the auth cookie;
    // in tests stuff a ClaimsPrincipal in manually so User.FindFirst(ClaimTypes.Name) works.
    private static BookingController ControllerAs(AppDbContext db, string username)
    {
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, username)], "Test");
        var controller = new BookingController(db)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            }
        };
        return controller;
    }

    // The controller returns an anonymous object like `new { success = true/false, ... }`
    // wrapped in a JsonResult
    private static bool ReadSuccess(IActionResult result)
    {
        var json = Assert.IsType<JsonResult>(result);
        var value = json.Value!;
        var prop = value.GetType().GetProperty("success")!;
        return (bool)prop.GetValue(value)!;
    }

    // GetBookedSlots returns `new { bookedSlots = List<string> }` — read the list out.
    private static List<string> ReadBookedSlots(IActionResult result)
    {
        var json = Assert.IsType<JsonResult>(result);
        var value = json.Value!;
        var prop = value.GetType().GetProperty("bookedSlots")!;
        return (List<string>)prop.GetValue(value)!;
    }

    // Mirrors what ConfirmBooking does before saving: treat the date/time as local,
    // then convert to UTC.
    private static DateTime LocalToUtc(int year, int month, int day, int hour)
    {
        var local = new DateTime(year, month, day, hour, 0, 0);
        return DateTime.SpecifyKind(local, DateTimeKind.Local).ToUniversalTime();
    }

    // No auth needed for GetBookedSlots — it doesn't read HttpContext.User.
    private static BookingController PlainController(AppDbContext db)
        => new(db) { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };

    // Security test: one user must NOT be able to cancel another user's booking.
    // If this ever fails, we have an authorization bug
    [Fact]
    public void CancelBooking_UserCannotCancelAnotherUsersBooking()
    {
        using var db = NewDb();

        // Arrange: two separate users, and a booking owned by Bob.
        var alice = new User { Username = "alice@x.com", First = "A", Last = "A", Password = "p", Description = "", CSULBID = "" };
        var bob   = new User { Username = "bob@x.com",   First = "B", Last = "B", Password = "p", Description = "", CSULBID = "" };
        db.Users.AddRange(alice, bob);
        db.SaveChanges();

        var bobsBooking = new Booking
        {
            UserID = bob.Id,
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddHours(1),
            Description = "Lib|101"
        };
        db.Bookings.Add(bobsBooking);
        db.SaveChanges();

        // Act: Alice is logged in and tries to cancel Bob's booking by its ID.
        var controller = ControllerAs(db, "alice@x.com");
        var result = controller.CancelBooking(bobsBooking.Id);

        // Assert: the controller refuses, and Bob's booking is still in the DB.
        Assert.False(ReadSuccess(result));
        Assert.NotNull(db.Bookings.Find(bobsBooking.Id));
    }

    // Happy-path counterpart to the security test above:
    // a user should be able to cancel their own booking.
    [Fact]
    public void CancelBooking_UserCanCancelOwnBooking()
    {
        using var db = NewDb();

        // Arrange: Alice exists and owns a booking.
        var alice = new User { Username = "alice@x.com", First = "A", Last = "A", Password = "p", Description = "", CSULBID = "" };
        db.Users.Add(alice);
        db.SaveChanges();

        var booking = new Booking
        {
            UserID = alice.Id,
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddHours(1),
            Description = "Lib|101"
        };
        db.Bookings.Add(booking);
        db.SaveChanges();

        // Act: Alice cancels her own booking.
        var controller = ControllerAs(db, "alice@x.com");
        var result = controller.CancelBooking(booking.Id);

        // Assert: success response and the row is gone from the DB.
        Assert.True(ReadSuccess(result));
        Assert.Null(db.Bookings.Find(booking.Id));
    }

    // ===== GetBookedSlots =====
    // These tests verify the filter correctly isolates bookings by building, room, and day.
    // All three matter, any bug here means the UI shows wrong availability.

    // Only bookings for the requested building should come back.
    [Fact]
    public void GetBookedSlots_ReturnsOnlyMatchingBuilding()
    {
        using var db = NewDb();

        // Arrange: same room number in two different buildings, both booked at different times.
        var alice = new User { Username = "a@x.com", First = "A", Last = "A", Password = "p", Description = "", CSULBID = "" };
        db.Users.Add(alice);
        db.SaveChanges();

        db.Bookings.Add(new Booking { UserID = alice.Id, StartTime = LocalToUtc(2026, 4, 18, 10), EndTime = LocalToUtc(2026, 4, 18, 11), Description = "Library|101" });
        db.Bookings.Add(new Booking { UserID = alice.Id, StartTime = LocalToUtc(2026, 4, 18, 14), EndTime = LocalToUtc(2026, 4, 18, 15), Description = "Engineering|101" });
        db.SaveChanges();

        // Act: ask for Library room 101.
        var result = PlainController(db).GetBookedSlots("Library", "101", "2026-04-18");

        // Assert: only the Library slot (10:00) comes back, not the Engineering one (14:00).
        var slots = ReadBookedSlots(result);
        Assert.Single(slots);
        Assert.Equal("10:00", slots[0]);
    }

    // Only bookings for the requested room should come back.
    [Fact]
    public void GetBookedSlots_ReturnsOnlyMatchingRoom()
    {
        using var db = NewDb();

        // Arrange: same building, two different rooms, both booked at different times.
        var alice = new User { Username = "a@x.com", First = "A", Last = "A", Password = "p", Description = "", CSULBID = "" };
        db.Users.Add(alice);
        db.SaveChanges();

        db.Bookings.Add(new Booking { UserID = alice.Id, StartTime = LocalToUtc(2026, 4, 18, 10), EndTime = LocalToUtc(2026, 4, 18, 11), Description = "Library|101" });
        db.Bookings.Add(new Booking { UserID = alice.Id, StartTime = LocalToUtc(2026, 4, 18, 14), EndTime = LocalToUtc(2026, 4, 18, 15), Description = "Library|202" });
        db.SaveChanges();

        // Act: ask for Library room 101.
        var result = PlainController(db).GetBookedSlots("Library", "101", "2026-04-18");

        // Assert: only the room 101 slot (10:00) comes back, not the room 202 one (14:00).
        var slots = ReadBookedSlots(result);
        Assert.Single(slots);
        Assert.Equal("10:00", slots[0]);
    }

    // Only bookings for the requested day should come back (same room, different day).
    [Fact]
    public void GetBookedSlots_ReturnsOnlyMatchingDay()
    {
        using var db = NewDb();

        // Arrange: same room, one booking today and one tomorrow.
        var alice = new User { Username = "a@x.com", First = "A", Last = "A", Password = "p", Description = "", CSULBID = "" };
        db.Users.Add(alice);
        db.SaveChanges();

        db.Bookings.Add(new Booking { UserID = alice.Id, StartTime = LocalToUtc(2026, 4, 18, 10), EndTime = LocalToUtc(2026, 4, 18, 11), Description = "Library|101" });
        db.Bookings.Add(new Booking { UserID = alice.Id, StartTime = LocalToUtc(2026, 4, 19, 10), EndTime = LocalToUtc(2026, 4, 19, 11), Description = "Library|101" });
        db.SaveChanges();

        // Act: ask for the 18th.
        var result = PlainController(db).GetBookedSlots("Library", "101", "2026-04-18");

        // Assert: only the 18th's slot comes back, not the 19th's.
        var slots = ReadBookedSlots(result);
        Assert.Single(slots);
        Assert.Equal("10:00", slots[0]);
    }

    // No matches at all = empty list, not null, not an error.
    [Fact]
    public void GetBookedSlots_ReturnsEmptyList_WhenNothingMatches()
    {
        // Arrange: empty DB, nothing seeded.
        using var db = NewDb();

        // Act: ask for any room/date.
        var result = PlainController(db).GetBookedSlots("Library", "101", "2026-04-18");

        // Assert: empty list (not null, no crash).
        Assert.Empty(ReadBookedSlots(result));
    }

    // The existing guard at BookingController.cs:130 catches unparseable date strings.
    // This test locks that guard in — if someone removes it, DateTime.Parse will throw
    // and the API will error instead of returning a clean empty list.
    [Fact]
    public void GetBookedSlots_ReturnsEmptyList_WhenDateIsInvalid()
    {
        // Arrange: empty DB, test is about the date parser.
        using var db = NewDb();

        // Act: pass a garbage date string.
        var result = PlainController(db).GetBookedSlots("Library", "101", "not-a-date");

        // Assert: controller returns an empty list instead of throwing.
        Assert.Empty(ReadBookedSlots(result));
    }

    // The frontend compares these strings against 24-hour button values to grey them out.
    // If the format ever drifts (e.g. to "2:00 PM"), the comparison stops matching
    // and every slot will appear available even when they aren't.
    [Fact]
    public void GetBookedSlots_FormatsTimeAs24Hour()
    {
        using var db = NewDb();

        // Arrange: a booking at 2:00 PM local time.
        var alice = new User { Username = "a@x.com", First = "A", Last = "A", Password = "p", Description = "", CSULBID = "" };
        db.Users.Add(alice);
        db.SaveChanges();

        db.Bookings.Add(new Booking { UserID = alice.Id, StartTime = LocalToUtc(2026, 4, 18, 14), EndTime = LocalToUtc(2026, 4, 18, 15), Description = "Library|101" });
        db.SaveChanges();

        // Act: fetch the booked slots for that day.
        var result = PlainController(db).GetBookedSlots("Library", "101", "2026-04-18");

        // Assert: the slot is formatted "14:00" (not "2:00 PM").
        Assert.Equal(new[] { "14:00" }, ReadBookedSlots(result));
    }

    // ===== ConfirmBooking =====
    // Double-booking prevention: if Bob already owns Library 101 at 2 PM,
    // Alice must not be able to book the same slot. The UI grays out the button,
    // but a user bypassing the UI (curl, devtools, race condition) would succeed
    // without a server-side guard. This test currently fails.
    [Fact]
    public void ConfirmBooking_RejectsDoubleBooking()
    {
        using var db = NewDb();

        // Arrange: two users; Bob already holds Library 101 on 2026-04-18 at 2 PM.
        var alice = new User { Username = "alice@x.com", First = "A", Last = "A", Password = "p", Description = "", CSULBID = "" };
        var bob   = new User { Username = "bob@x.com",   First = "B", Last = "B", Password = "p", Description = "", CSULBID = "" };
        db.Users.AddRange(alice, bob);
        db.SaveChanges();

        db.Bookings.Add(new Booking
        {
            UserID = bob.Id,
            StartTime = LocalToUtc(2026, 4, 18, 14),
            EndTime   = LocalToUtc(2026, 4, 18, 15),
            Description = "Library|101"
        });
        db.SaveChanges();

        // Act: Alice is logged in and tries to book the exact same slot.
        var controller = ControllerAs(db, "alice@x.com");
        var result = controller.ConfirmBooking("Library", "101", "2026-04-18", "14:00-15:00");

        // Assert: request is rejected and no second booking was created.
        Assert.False(ReadSuccess(result));
        Assert.Equal(1, db.Bookings.Count());
    }
}
