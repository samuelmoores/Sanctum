using Microsoft.AspNetCore.Mvc;
using Sanctum.Models;
using System.Diagnostics;
using Sanctum.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;

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
        // PROFILE WEBPAGE
        // =====================================================

        [HttpGet]
        public IActionResult Profile()
        {
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            if (username == null) return RedirectToAction("Login");

            var user = _db.Users.FirstOrDefault(u => u.Username == username);
            if (user == null) return RedirectToAction("Login");

            ViewBag.use = user.Username;
            ViewBag.fir = user.First;
            ViewBag.las = user.Last;
            ViewBag.des = user.Description;
            ViewBag.intel = user.CSULBID;
            ViewBag.password = user.Password;
            ViewBag.userId = user.Id;
            ViewBag.hasPhoto = user.ProfilePhoto != null;

            return View();
        }

        [HttpPost]
        public IActionResult Profile(string? Password, string? CSULBID, string? Description)
        {
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            if (username == null) return RedirectToAction("Login");

            var user = _db.Users.FirstOrDefault(u => u.Username == username);
            if (user == null) return RedirectToAction("Login");

            if (!string.IsNullOrWhiteSpace(Password))
                user.Password = Password;
            if (!string.IsNullOrWhiteSpace(CSULBID))
                user.CSULBID = CSULBID;
            if (!string.IsNullOrWhiteSpace(Description))
                user.Description = Description;

            _db.SaveChanges();
            return RedirectToAction("Profile");
        }

        [HttpPost]
        public async Task<IActionResult> UploadImage(IFormFile upload)
        {
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            if (username == null) return RedirectToAction("Login");

            var user = _db.Users.FirstOrDefault(u => u.Username == username);
            if (user == null || upload == null || upload.Length == 0)
                return RedirectToAction("Profile");

            using var ms = new MemoryStream();
            await upload.CopyToAsync(ms);
            user.ProfilePhoto = ms.ToArray();
            user.ProfilePhotoType = upload.ContentType;
            _db.SaveChanges();

            return RedirectToAction("Profile");
        }

        [HttpGet]
        public IActionResult GetPhoto(int id)
        {
            var user = _db.Users.Find(id);
            if (user?.ProfilePhoto == null) return NotFound();
            return File(user.ProfilePhoto, user.ProfilePhotoType ?? "image/jpeg");
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
        public async Task<IActionResult> Register(string email, string First, string Last, string password)
        {
            var user = new User { Email = email, Username = email, First = First, Last = Last, Password = password, Description = "", CSULBID = "" };

            Console.WriteLine("email to register with: " + email);

            if(email.Contains("csulb.edu"))
            {
                _db.Users.Add(user);
                _db.SaveChanges();
                return await Login(email, password);
            }

            return RedirectToAction("Register");

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

            var bookingUsername = User.FindFirst(ClaimTypes.Name)?.Value ?? "Student";
            ViewBag.UserName = bookingUsername;

            var bookingUser = _db.Users.FirstOrDefault(u => u.Username == bookingUsername);
            ViewBag.userId = bookingUser?.Id;
            ViewBag.hasPhoto = bookingUser?.ProfilePhoto != null;

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