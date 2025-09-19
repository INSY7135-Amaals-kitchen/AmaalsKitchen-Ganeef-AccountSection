using System.Diagnostics;
using AmaalsKitchen.Models;
using Microsoft.AspNetCore.Mvc;

namespace AmaalsKitchen.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Menu()
        {
            return View();
        }

        public IActionResult About()
        {
            ViewData["Title"] = "About - Amaals Kitchen";
            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Title"] = "Contact - Amaals Kitchen";
            return View();
        }

        public IActionResult Location()
        {
            ViewData["Title"] = "Location - Amaals Kitchen";
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}