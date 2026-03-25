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
        // ===== DATABASE CONTEXT =====
        private readonly AppDbContext _db;

        public HomeController(AppDbContext db)
        {
            _db = db;
        }

        // =====================================================
        // HOME PAGE
        // =====================================================
        public IActionResult Index()
        {
            return View();
        }

        // =====================================================
        // PROFILE INPUT (NOT FULLY CONNECTED TO UI YET)
        // =====================================================

        // Made few Changes to Min handles profile submission (currently not fully wired)
        [HttpPost]
        public IActionResult Profile()
        {
            ViewData["Title"] = "Profile";
            var info = new User();

            Console.WriteLine("Add School ID:");
            string idNum = info.CSULBID.ToString();
            idNum = Console.ReadLine();

            Console.WriteLine("Add Description");
            info.Description = Console.ReadLine();

            // NOTE: these methods do not actually return user input correctly in web context
            Console.WriteLine($"Description: @{info.Description}");
            Console.WriteLine($"CSULB ID: @{idNum}");
            return View(info);
        }

        // =====================================================
        // LOGIN SYSTEM
        // =====================================================

        // GET login page
        public IActionResult Login()
        {
            return View();
        }

        // POST login (basic username/password check)
        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            Console.WriteLine(username + ": " + password);

            var user = _db.Users.FirstOrDefault(u => u.Username == username && u.Password == password);

            if (user == null)
            {
                return View(); // login failed
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),      // Stores username in claims for later retrieval
                // Stores email in claims for later use since we dont have a seperate email field
                new Claim(ClaimTypes.Email, user.Username)      
            };

            // creates authentication cookie with user claims
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            // creates principal object to sign in the user
            var principal = new ClaimsPrincipal(identity);

            // signs in the user by setting the authentication cookie so the server can recognize them
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        
            return RedirectToAction("Booking"); // login succeeded
        }

        // =====================================================
        // GOOGLE AUTHENTICATION (EXTERNAL LOGIN)
        // =====================================================

        // starts Google login flow
        [HttpPost]
        public IActionResult ExternalLogin(string provider, string returnUrl = "/")
        {
            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Home", new { returnUrl });
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };

            return Challenge(properties, provider);
        }

        // handles Google login callback
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

            // extract user info from Google
            var email = result.Principal.FindFirstValue(ClaimTypes.Email);
            var name = result.Principal.FindFirstValue(ClaimTypes.Name);

            // currently just redirects (no DB save yet)
            return RedirectToAction("Booking");
        }

        // logout user
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        // =====================================================
        // REGISTRATION
        // =====================================================

        // GET register page
        public IActionResult Register()
        {
            return View();
        }

        // POST register (adds user to database)
        [HttpPost]
        public IActionResult Register(string username, string password)
        {
            var user = new User { Username = username, Password = password };

            _db.Users.Add(user);
            _db.SaveChanges();

            return RedirectToAction("Login");
        }

        // =====================================================
        // BOOKING PAGE
        // =====================================================

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

            // gets the signed-in user's name from claims
            ViewBag.UserName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Student";

            return View();
        }

        // =====================================================
        // MISC / DEFAULT
        // =====================================================

        public IActionResult Privacy()
        {
            return View();
        }

        // error handling
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel 
            { 
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier 
            });
        }
    }
}