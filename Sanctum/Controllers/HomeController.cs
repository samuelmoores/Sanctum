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

        // allows user to add a description (currently console-based, not UI)
        public IActionResult Describe() 
        {
            var des = new User();

            Console.Write("Add Description: ");
            des.Description = Console.ReadLine();

            Console.WriteLine($"Description@{des.Description}");

            return View();
        }

        // allows user to add CSULB ID (currently console-based, not UI)
        public IActionResult ID() 
        {
            var id = new User();

            Console.Write("Add CSULB ID: ");
            string idNum = id.CSULBID.ToString();

            idNum = Console.ReadLine();

            Console.WriteLine($"CSULB ID: @{id.CSULBID}");

            return View();
        }

        // handles profile submission (currently not fully wired)
        [HttpPost]
        public IActionResult Profile()
        {
            ViewData["Title"] = "Profile";

            // NOTE: these methods do not actually return user input correctly in web context
            Console.WriteLine(Describe());
            Console.WriteLine(ID());

            return RedirectToAction("Booking", "Profile");
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
        public IActionResult Login(string username, string password)
        {
            Console.WriteLine(username + ": " + password);

            var user = _db.Users.FirstOrDefault(u => u.Username == username && u.Password == password);

            if (user == null)
            {
                return View(); // login failed
            }
        
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
            // temporary booking object (not connected to UI yet)
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