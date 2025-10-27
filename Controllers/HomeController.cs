using AmaalsKitchen.Data;
using AmaalsKitchen.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Linq;

namespace AmaalsKitchen.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Menu()
        {
            var products = _context.Products.ToList();
            return View(products);  // send product list to Menu.cshtml
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
