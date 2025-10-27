using AmaalsKitchen.Data;
using AmaalsKitchen.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System;
using System.Linq;

namespace AmaalsKitchen.Controllers
{
    public class AccountController : Controller
    {
        private readonly ILogger<AccountController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly PasswordHasher<User> _passwordHasher;

        public AccountController(ILogger<AccountController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
            _passwordHasher = new PasswordHasher<User>();
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
                if (model.Email == "admin@amaalskitchen.com" && model.Password == "Admin123")
                {
                    HttpContext.Session.SetString("UserEmail", model.Email);
                    HttpContext.Session.SetString("UserName", "Admin");
                    HttpContext.Session.SetString("UserRole", "Admin");

                    TempData["SuccessMessage"] = "Welcome Admin!";
                    return RedirectToAction("AdminDashboard", "Admins");
                }

                var user = _context.Users.FirstOrDefault(u => u.Email == model.Email);
                if (user != null)
                {
                    var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.Password);

                    if (result == PasswordVerificationResult.Success)
                    {
                        user.LastLoginDate = DateTime.UtcNow;
                        _context.SaveChanges();

                        HttpContext.Session.SetString("UserEmail", user.Email);
                        HttpContext.Session.SetString("UserName", user.FirstName);
                        HttpContext.Session.SetString("UserRole", "User");

                        TempData["SuccessMessage"] = "Login successful!";
                        return RedirectToAction("Index", "Home");
                    }
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
                    CreatedDate = DateTime.UtcNow
                };

                user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);

                _context.Users.Add(user);
                _context.SaveChanges();

                TempData["SuccessMessage"] = "Registration successful! Please login.";
                return RedirectToAction("Login");
            }

            return View(model);
        }

        // ===================== RESET PASSWORD =====================
        [HttpGet]
        public IActionResult ResetPassword() => View();

        [HttpPost]
        public IActionResult ResetPassword(ResetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = _context.Users.FirstOrDefault(u => u.Email == model.Email);
                if (user == null)
                {
                    ModelState.AddModelError("", "No account found with that email.");
                    return View(model);
                }

                user.PasswordHash = _passwordHasher.HashPassword(user, model.NewPassword);
                _context.SaveChanges();

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
                return RedirectToAction("Login");

            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            if (user == null)
                return NotFound();

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
            var sessionEmail = HttpContext.Session.GetString("UserEmail");
            var emailToUse = sessionEmail ?? model?.Email;

            if (string.IsNullOrEmpty(emailToUse))
            {
                TempData["ErrorMessage"] = "Your session expired. Please log in again.";
                return RedirectToAction("Login");
            }

            if (!ModelState.IsValid)
                return View(model);

            var user = _context.Users.FirstOrDefault(u => u.Email == emailToUse);
            if (user == null)
                return NotFound();

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.PhoneNumber = model.PhoneNumber;

            _context.SaveChanges();
            HttpContext.Session.SetString("UserName", user.FirstName);

            TempData["SuccessMessage"] = "Profile updated successfully!";
            return RedirectToAction("Profile");
        }

        // ===================== AJAX EDIT PROFILE =====================
        [HttpPost]
        public IActionResult EditProfileAjax([FromBody] EditProfileViewModel model)
        {
            var sessionEmail = HttpContext.Session.GetString("UserEmail");
            var emailToUse = sessionEmail ?? model?.Email;

            if (string.IsNullOrEmpty(emailToUse))
                return Json(new { success = false, message = "Session expired. Please login again." });

            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Invalid input. Please check all fields." });

            var user = _context.Users.FirstOrDefault(u => u.Email == emailToUse);
            if (user == null)
                return Json(new { success = false, message = "User not found." });

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.PhoneNumber = model.PhoneNumber;

            _context.SaveChanges();
            HttpContext.Session.SetString("UserName", user.FirstName);

            return Json(new
            {
                success = true,
                message = "Profile updated successfully!",
                firstName = user.FirstName,
                lastName = user.LastName,
                phoneNumber = user.PhoneNumber
            });
        }
    }
}
