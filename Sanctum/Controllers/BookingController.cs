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
                // No building since one is already associated with the room, so we can just store the room info in the description for simplicity
                Description = $"{room}" // Store room info in description
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
            var bookings = _db.Bookings
                .Where(b => b.UserID == user.Id)
                .OrderByDescending(b => b.StartTime)
                .Select(b => new
                {
                    description = b.Description,
                    // Format example: March 24, 2026 at 2:30 PM - 3:30 PM
                    startTime = b.StartTime.ToString("MMMM d, yyyy 'at' h:mm tt"),
                    endTime = b.EndTime.ToString("h:mm tt")
                })
                .ToList();

            return Json(new { success = true, bookings });
        }
    }
}