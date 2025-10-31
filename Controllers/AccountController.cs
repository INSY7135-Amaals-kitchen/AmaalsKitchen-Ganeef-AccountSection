/// <summary>
/// AccountController handles all user authentication and profile management operations.
/// This includes user registration, login/logout, password reset, and profile editing.
/// Uses ASP.NET Identity PasswordHasher for secure password storage.
/// Session-based authentication is implemented for maintaining user state.
/// </summary>
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

        /// <summary>
        /// Initializes the AccountController with required dependencies.
        /// </summary>
        /// <param name="logger">Logger for tracking application events</param>
        /// <param name="context">Database context for user data operations</param>
        public AccountController(ILogger<AccountController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
            _passwordHasher = new PasswordHasher<User>();
        }

        // ===================== LOGIN =====================

        /// <summary>
        /// Displays the login page.
        /// </summary>
        /// <returns>Login view</returns>
        [HttpGet]
        public IActionResult Login() => View();

        /// <summary>
        /// Authenticates user credentials and creates a session.
        /// Supports both admin (hardcoded) and regular user authentication.
        /// Admin credentials: admin@amaalskitchen.com / Admin123
        /// Regular users are authenticated against the database using hashed passwords.
        /// </summary>
        /// <param name="model">Login form data containing email and password</param>
        /// <returns>Redirects to appropriate dashboard on success, returns view with errors on failure</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check for hardcoded admin credentials
                if (model.Email == "admin@amaalskitchen.com" && model.Password == "Admin123")
                {
                    // Create admin session
                    HttpContext.Session.SetString("UserEmail", model.Email);
                    HttpContext.Session.SetString("UserName", "Admin");
                    HttpContext.Session.SetString("UserRole", "Admin");

                    TempData["SuccessMessage"] = "Welcome Admin!";
                    return RedirectToAction("AdminDashboard", "Admins");
                }

                // Authenticate regular user from database
                var user = _context.Users.FirstOrDefault(u => u.Email == model.Email);
                if (user != null)
                {
                    // Verify hashed password using ASP.NET Identity PasswordHasher
                    var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.Password);

                    if (result == PasswordVerificationResult.Success)
                    {
                        // Update last login timestamp
                        user.LastLoginDate = DateTime.UtcNow;
                        _context.SaveChanges();

                        // Create user session
                        HttpContext.Session.SetString("UserEmail", user.Email);
                        HttpContext.Session.SetString("UserName", user.FirstName);
                        HttpContext.Session.SetString("UserRole", "User");

                        TempData["SuccessMessage"] = "Login successful!";
                        return RedirectToAction("Index", "Home");
                    }
                }

                // Invalid credentials
                ModelState.AddModelError("", "Invalid email or password");
            }

            return View(model);
        }

        // ===================== REGISTER =====================

        /// <summary>
        /// Displays the registration page.
        /// </summary>
        /// <returns>Registration view</returns>
        [HttpGet]
        public IActionResult Register() => View();

        /// <summary>
        /// Creates a new user account with hashed password.
        /// Validates that email is not already registered.
        /// </summary>
        /// <param name="model">Registration form data containing user details and password</param>
        /// <returns>Redirects to login on success, returns view with errors on failure</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check if email already exists
                if (_context.Users.Any(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "Email is already registered.");
                    return View(model);
                }

                // Create new user object
                var user = new User
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber,
                    CreatedDate = DateTime.UtcNow
                };

                // Hash password before storing (security best practice)
                user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);

                // Save to database
                _context.Users.Add(user);
                _context.SaveChanges();

                TempData["SuccessMessage"] = "Registration successful! Please login.";
                return RedirectToAction("Login");
            }

            return View(model);
        }

        // ===================== RESET PASSWORD =====================

        /// <summary>
        /// Displays the password reset page.
        /// </summary>
        /// <returns>Password reset view</returns>
        [HttpGet]
        public IActionResult ResetPassword() => View();

        /// <summary>
        /// Resets user password to a new value.
        /// NOTE: In production, this should use token-based password reset via email.
        /// </summary>
        /// <param name="model">Password reset data containing email and new password</param>
        /// <returns>Redirects to login on success, returns view with errors on failure</returns>
        [HttpPost]
        public IActionResult ResetPassword(ResetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Find user by email
                var user = _context.Users.FirstOrDefault(u => u.Email == model.Email);
                if (user == null)
                {
                    ModelState.AddModelError("", "No account found with that email.");
                    return View(model);
                }

                // Hash and save new password
                user.PasswordHash = _passwordHasher.HashPassword(user, model.NewPassword);
                _context.SaveChanges();

                TempData["SuccessMessage"] = "Your password has been reset successfully! You can log in now.";
                return RedirectToAction("Login");
            }

            return View(model);
        }

        // ===================== LOGOUT =====================

        /// <summary>
        /// Logs out the current user by clearing their session data.
        /// </summary>
        /// <returns>Redirects to home page</returns>
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["InfoMessage"] = "You have been logged out.";
            return RedirectToAction("Index", "Home");
        }

        // ===================== VIEW PROFILE =====================

        /// <summary>
        /// Displays the current user's profile information.
        /// Requires active user session.
        /// </summary>
        /// <returns>Profile view with user data, or redirects to login if not authenticated</returns>
        [HttpGet]
        public IActionResult Profile()
        {
            // Check if user is logged in
            var email = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(email))
                return RedirectToAction("Login");

            // Retrieve user from database
            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            if (user == null)
                return NotFound();

            return View(user);
        }

        // ===================== EDIT PROFILE (GET) =====================

        /// <summary>
        /// Displays the profile editing form pre-filled with current user data.
        /// Requires active user session.
        /// </summary>
        /// <returns>Edit profile view, or redirects to login if not authenticated</returns>
        [HttpGet]
        public IActionResult EditProfile()
        {
            var email = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(email))
                return RedirectToAction("Login");

            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            if (user == null)
                return NotFound();

            // Map user data to view model
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

        /// <summary>
        /// Updates user profile information in the database.
        /// Also updates the session to reflect name changes.
        /// </summary>
        /// <param name="model">Updated profile data</param>
        /// <returns>Redirects to profile view on success, returns form with errors on failure</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditProfile(EditProfileViewModel model)
        {
            // Retrieve user identifier from session or model
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

            // Update user properties
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.PhoneNumber = model.PhoneNumber;

            _context.SaveChanges();

            // Update session with new name
            HttpContext.Session.SetString("UserName", user.FirstName);

            TempData["SuccessMessage"] = "Profile updated successfully!";
            return RedirectToAction("Profile");
        }

        // ===================== AJAX EDIT PROFILE =====================

        /// <summary>
        /// AJAX endpoint for updating profile without page reload.
        /// Returns JSON response with success status and updated data.
        /// </summary>
        /// <param name="model">Profile data from AJAX request</param>
        /// <returns>JSON object with success status and user data</returns>
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

            // Update user data
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.PhoneNumber = model.PhoneNumber;

            _context.SaveChanges();
            HttpContext.Session.SetString("UserName", user.FirstName);

            // Return updated data to client
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