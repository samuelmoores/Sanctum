using Microsoft.AspNetCore.Mvc;
using Sanctum.Data;
using Sanctum.Models;
using System.Security.Claims;
using System.Linq;
using System.Diagnostics;

namespace Sanctum.Controllers
{
    public class BookingController : Controller
    {
        // ===== DATABASE CONTEXT =====
        private readonly AppDbContext _db;

        public BookingController(AppDbContext db)
        {
            _db = db;
        }
        
        [HttpPost]
        public IActionResult ConfirmBooking(string building, string room, string date, string time)
        {

            // Get the logged-in user
            var username = User.FindFirst(ClaimTypes.Name)?.Value ?? 
            User.FindFirst(ClaimTypes.Email)?.Value;

            // Retrieve the user from the database using the usernam
            var user = _db.Users.FirstOrDefault(u => u.Username == username);


            // If user is not found, return an error response
            if (user == null)
            {
                return Json(new { success = false, message = "User not found." });
            }            
    
            // Parse the start time from date and time strings (1 Hours Booking)
            // Needed to add .ToUniversalTime() to ensure the time is stored in UTC format in the database becasue while SQlite stores in local time, 
            // PostgreSQL (Supabase) stores in UTC which causes issues if not addressed

            //Explicitly set the Kind before conversion to ensure it is treated as local time and then converted to UTC correctly
            var timePart = time.Split('-')[0];
            var dateTimeString = $"{date} {timePart}";
            DateTime startTime = DateTime.Parse(dateTimeString);

            startTime = DateTime.SpecifyKind(startTime, DateTimeKind.Local).ToUniversalTime();
            DateTime endTime = startTime.AddHours(1);

            // Create a new booking and save it to the database
            var booking = new Booking
            {
                UserID = user.Id,
                StartTime = startTime,
                EndTime = endTime,
                Description = $"{building}|{room}" // Store building and room separated by pipe
            };

            _db.Bookings.Add(booking);
            _db.SaveChanges();

            return Json(new { success = true, message = "Booking confirmed." });
        }

        [HttpGet]
        public IActionResult GetMyBookings()
        {
            // Get the logged-in user
            var username = User.FindFirst(ClaimTypes.Name)?.Value ??
                User.FindFirst(ClaimTypes.Email)?.Value;

            // Retrieve the user from the database
            var user = _db.Users.FirstOrDefault(u => u.Username == username);

            if (user == null)
            {
                return Json(new { success = false, message = "User not found." });
            }

            // Fetch all bookings for this user (ordered by descending time)
            var rawBookings = _db.Bookings
                .Where(b => b.UserID == user.Id)
                .OrderByDescending(b => b.StartTime)
                .ToList();

            var bookings = rawBookings.Select(b =>
            {
                var parts = b.Description?.Split('|');
                var building = parts?.Length == 2 ? parts[0] : "—";
                var room     = parts?.Length == 2 ? parts[1] : (b.Description ?? "—");
                return new
                {
                    id = b.Id,
                    building,
                    room,
                    date = b.StartTime.ToString("MMMM d, yyyy"),
                    time = b.StartTime.ToString("h:mm tt") + " – " + b.EndTime.ToString("h:mm tt")
                };
            }).ToList();

            return Json(new { success = true, bookings });
        }
    

        [HttpDelete]
        public IActionResult CancelBooking(int id)
        {
            var username = User.FindFirst(ClaimTypes.Name)?.Value ??
                User.FindFirst(ClaimTypes.Email)?.Value;

            var user = _db.Users.FirstOrDefault(u => u.Username == username);
            if (user == null)
                return Json(new { success = false, message = "User not found." });

            var booking = _db.Bookings.FirstOrDefault(b => b.Id == id && b.UserID == user.Id);
            if (booking == null)
                return Json(new { success = false, message = "Booking not found." });

            _db.Bookings.Remove(booking);
            _db.SaveChanges();

            return Json(new { success = true });
        }

        // Fetch booked time slots for a specific building, room, and date to disable those time slots on the front end
        [HttpGet]
        public IActionResult GetBookedSlots(string building, string room, string date)
        {
            // Validate the date input
            if (!DateTime.TryParse(date, out DateTime parsedDate))
            {
                return Json(new { bookedSlots = new List<string>() });
            }

            // Fetch all bookings for the specified building, room, and date
            DateTime dayStart = DateTime.SpecifyKind(parsedDate.Date, DateTimeKind.Local).ToUniversalTime();
            DateTime dayEnd = dayStart.AddDays(1);

            // Filter bookings by matching building and room in the description and falling within the specified date range
            var booked = _db.Bookings
                .Where(b => 
                    b.Description == $"{building}|{room}" &&
                    b.StartTime >= dayStart && 
                    b.StartTime < dayEnd)
                .Select(b => b.StartTime)
                .ToList();

            // Convert booked DateTimes to 24-hour format strings for easier comparison on the front end
            var bookedSlots = booked
                .Select(dt => dt.ToLocalTime().ToString("HH:mm")) // Convert back to local time for display
                .ToList();

            return Json(new { bookedSlots = bookedSlots });
        }

    }

}