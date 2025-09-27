using Microsoft.AspNetCore.Mvc;

namespace AmaalsKitchen.Controllers
{
    public class AdminsController : Controller
    {
        public IActionResult AddProducts()
        {
            return View();
        }
        public IActionResult AdminDashboard()
        {
            return View();
        }
    }
}
