using Microsoft.AspNetCore.Mvc;
using Sanctum.Models;
using System.Diagnostics;
using Sanctum.Data;

namespace Sanctum.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _db;

        public HomeController(AppDbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            return View();
        }
        
        public IActionResult Login()
        {
            return View();
        }
        
        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            Console.WriteLine(username + ": "  + password);
            var user = _db.Users.FirstOrDefault(u => u.Username == username && u.Password == password);
            if (user == null)
            {
                return View(); // login failed
            }
        
            return RedirectToAction("Booking"); // login succeeded
        }
        
        public IActionResult Register()
        {
            return View();
        }
        
        [HttpPost]
        public IActionResult Register(string username, string password)
        {
            var user = new User { Username = username, Password = password };
            _db.Users.Add(user);
            _db.SaveChanges();
            return RedirectToAction("Login");
        }
        
        public IActionResult Booking()
        {
            return View();
        }


        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
