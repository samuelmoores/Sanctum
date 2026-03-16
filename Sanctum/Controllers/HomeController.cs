using Microsoft.AspNetCore.Mvc;
using Sanctum.Models;
using System.Diagnostics;
using Sanctum.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

using System.Reflection.Metadata.Ecma335;

namespace Sanctum.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _db;

        public HomeController(AppDbContext db)
        {
            _db = db;
        }

        public IActionResult Describe() 
        {
            var des = new User();
            Console.Write("Add Description: ");
            des.Description = Console.ReadLine();
            Console.WriteLine($"Description@{des.Description}");
            return View();
        }

        public IActionResult ID() 
        {
            var id = new User();
            Console.Write("Add CSULB ID: ");
            string idNum = id.CSULBID.ToString();
            idNum = Console.ReadLine();
            Console.WriteLine($"CSULB ID: @{id.CSULBID}");
            return View();
        }

  
        [HttpPost]
        public IActionResult Profile()
        {
            ViewData["Title"] = "Profile";
            Console.WriteLine(Describe());
            Console.WriteLine(ID());

            return RedirectToAction("Booking", "Profile");
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

        // Google Authentication Methods (3)
        [HttpPost]
        public IActionResult ExternalLogin(string provider, string returnUrl = "/")
        {
            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Home", new { returnUrl });
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, provider);
        }

        [HttpGet]
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl = "/", string? remoteError = null)
        {
            if (remoteError != null)
            {
                return RedirectToAction(nameof(Login));
            }

            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            if (!result.Succeeded)
            {
                return RedirectToAction(nameof(Login));
            }

            var email = result.Principal.FindFirstValue(ClaimTypes.Email);
            var name = result.Principal.FindFirstValue(ClaimTypes.Name);

            return RedirectToAction("Booking");
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
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
            var book = new Booking();
            book.StartTime = DateTime.Now;
            book.EndTime = book.StartTime;
            bool time = true;

            var result = book.EndTime - book.StartTime;

            if (time)
            {
                Console.WriteLine(result);
            }

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
