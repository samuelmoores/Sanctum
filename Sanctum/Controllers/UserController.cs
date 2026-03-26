using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Sanctum.Data;
using System.Security.Claims;

namespace Sanctum.Controllers
{
    public class UserController : Controller
    {
        private readonly AppDbContext _db;

        public UserController(AppDbContext _context)
        {
            this._db = _context;
        }

        [HttpPost]
        public async Task<IActionResult> UpdateEmail(IFormCollection collection)
        {
            var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var newEmail = collection["Email"].ToString().Trim();
            var user = _db.Users.Find(id);

            if (user == null)
            {
                throw new KeyNotFoundException($"User with ID {id} not found.");
            }

            if (_db.Users.Any(u => u.Username == newEmail && u.Id != id))
                throw new InvalidOperationException("Email is already in use by another user.");

            user.Username = newEmail;
            _db.SaveChanges();

            return RedirectToAction(nameof(HomeController.Index));
        }
    }
}
