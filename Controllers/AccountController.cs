using AmaalsKitchen.Data;
using AmaalsKitchen.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using System.Linq;

namespace AmaalsKitchen.Controllers
{
    public class AccountController : Controller
    {
        private readonly ILogger<AccountController> _logger;
        private readonly ApplicationDbContext _context;

        public AccountController(ILogger<AccountController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        // ===================== LOGIN =====================
        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = _context.Users
                    .FirstOrDefault(u => u.Email == model.Email && u.Password == model.Password);

                if (user != null)
                {
                    HttpContext.Session.SetString("UserEmail", user.Email);
                    HttpContext.Session.SetString("UserName", user.FirstName);

                    TempData["SuccessMessage"] = "Login successful!";
                    return RedirectToAction("Index", "Home");
                }

                ModelState.AddModelError("", "Invalid email or password");
            }

            return View(model);
        }

        // ===================== REGISTER =====================
        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (_context.Users.Any(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "Email is already registered.");
                    return View(model);
                }

                var user = new User
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber,
                    Password = model.Password // ⚠️ hash in production
                };

                _context.Users.Add(user);
                _context.SaveChanges();

                TempData["SuccessMessage"] = "Registration successful! Please login.";
                return RedirectToAction("Login");
            }

            return View(model);
        }
        [HttpGet]
        public IActionResult ResetPassword()
        {
            return View();
        }

        [HttpPost]
        public IActionResult ResetPassword(ResetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                // 1. Find user by email
                var user = _context.Users.FirstOrDefault(u => u.Email == model.Email);
                if (user == null)
                {
                    ModelState.AddModelError("", "No account found with that email.");
                    return View(model);
                }

                // 2. Update password (⚠️ Hash it in real apps, don’t store plain text)
                user.Password = model.NewPassword;
                _context.SaveChanges();

                // 3. Redirect with success
                TempData["SuccessMessage"] = "Your password has been reset successfully! You can log in now.";
                return RedirectToAction("Login");
            }

            return View(model);
        }




        // ===================== LOGOUT =====================
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["InfoMessage"] = "You have been logged out.";
            return RedirectToAction("Index", "Home");
        }

        // ===================== VIEW PROFILE =====================
        [HttpGet]
        public IActionResult Profile()
        {
            var email = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(email))
                return RedirectToAction("Login");

            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            if (user == null)
                return NotFound();

            return View(user);
        }

        // ===================== EDIT PROFILE (GET) =====================
        [HttpGet]
        public IActionResult EditProfile()
        {
            var email = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(email))
            {
                _logger.LogWarning("EditProfile(GET): no session email -> redirect to Login");
                return RedirectToAction("Login");
            }

            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            if (user == null)
            {
                _logger.LogWarning("EditProfile(GET): user not found for email {Email}", email);
                return NotFound();
            }

            var model = new EditProfileViewModel
            {
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber
            };

            return View(model);
        }

        // ===================== EDIT PROFILE (POST) =====================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditProfile(EditProfileViewModel model)
        {
            _logger.LogInformation("EditProfile(POST) called. SessionEmail={SessionEmail}, ModelEmail={ModelEmail}",
                HttpContext.Session.GetString("UserEmail"), model?.Email);

            var sessionEmail = HttpContext.Session.GetString("UserEmail");
            var emailToUse = sessionEmail ?? model?.Email;

            if (string.IsNullOrEmpty(emailToUse))
            {
                _logger.LogWarning("EditProfile(POST): no email available (session and model empty).");
                TempData["ErrorMessage"] = "Your session expired. Please log in again.";
                return RedirectToAction("Login");
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("EditProfile(POST): ModelState invalid. Listing errors:");
                foreach (var kvp in ModelState)
                {
                    foreach (var err in kvp.Value.Errors)
                        _logger.LogWarning(" - {Field}: {Error}", kvp.Key, err.ErrorMessage);
                }
                return View(model);
            }

            var user = _context.Users.FirstOrDefault(u => u.Email == emailToUse);
            if (user == null)
            {
                _logger.LogWarning("EditProfile(POST): user not found for email {Email}", emailToUse);
                return NotFound();
            }

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.PhoneNumber = model.PhoneNumber;

            _context.SaveChanges();

            HttpContext.Session.SetString("UserName", user.FirstName);

            TempData["SuccessMessage"] = "Profile updated successfully!";
            _logger.LogInformation("EditProfile(POST): Successfully updated profile for {Email}", emailToUse);

            return RedirectToAction("Profile");
        }

        // ===================== DEBUG AJAX ENDPOINT (for troubleshooting only) =====================
        // NOTE: This endpoint ignores antiforgery and exists only to help diagnose whether the browser can reach the server.
        // Remove or secure this in production.
        [HttpPost]
        [Route("Account/EditProfileAjax")]
        [IgnoreAntiforgeryToken]
        public IActionResult EditProfileAjax([FromBody] EditProfileViewModel model)
        {
            _logger.LogInformation("EditProfileAjax received (debug). Model: {@Model}", model);

            var sessionEmail = HttpContext.Session.GetString("UserEmail");
            var emailToUse = sessionEmail ?? model?.Email;
            if (string.IsNullOrEmpty(emailToUse))
            {
                return BadRequest("No email provided and session missing.");
            }

            var user = _context.Users.FirstOrDefault(u => u.Email == emailToUse);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            if (model != null)
            {
                user.FirstName = model.FirstName ?? user.FirstName;
                user.LastName = model.LastName ?? user.LastName;
                user.PhoneNumber = model.PhoneNumber ?? user.PhoneNumber;
                _context.SaveChanges();
                HttpContext.Session.SetString("UserName", user.FirstName);
                return Ok(new { message = "Saved via AJAX", email = emailToUse });
            }

            return BadRequest("Empty model.");
        }
    }
}
