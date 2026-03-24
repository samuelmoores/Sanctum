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
            DateTime startTime = DateTime.Parse($"{date} {time}");
            DateTime endTime = startTime.AddHours(1);

            // Create a new booking and save it to the database
            var booking = new Booking
            {
                UserID = user.Id,
                StartTime = startTime,
                EndTime = endTime,
                Description = $"{building} - {room}" // Store room info in description
            };

            _db.Bookings.Add(booking);
            _db.SaveChanges();

            return Json(new { success = true, message = "Booking confirmed." });
        }
    }
}