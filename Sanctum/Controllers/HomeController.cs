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
            return View();
        }

        [HttpPost]
        public IActionResult Profile(User info)
        {
            ViewData["Title"] = "Profile";

            ViewBag.use = info.Username;

            ViewBag.des = info.Description;

            //if (info.CSULBID.Length != 9 || !info.CSULBID.All(char.IsDigit))
            //{
            //    return Content("Invalid ID — must be exactly 9 digits.");
            //}
            ViewBag.intel = info.CSULBID;
           
            //try
            //{
            _db.Users.Add(info);
            _db.SaveChanges();
            //}
            //catch (Exception ex)
            //{
            //    return Content(ex.InnerException?.Message ?? ex.Message);
            //}
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UploadImg(IFormFile uploadedFile) 
        {
            string upload = Path.Combine(Directory.GetCurrentDirectory(),"wweroot/images/uploads");
            if (!Directory.Exists(upload)) Directory.CreateDirectory(upload);

            
            string unique = Guid.NewGuid().ToString() + "_" + Guid.NewGuid().ToString();
            string file = Path.Combine(upload, unique);

            using(var fileStreams = new FileStream(file, FileMode.Create))
            {
                await uploadedFile.CopyToAsync(fileStreams);
            }

            ViewBag.Mess = "Upload Successfull";

            return View("Profile");
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
            var user = new User { Username = username, Password = password, Description = "" };

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