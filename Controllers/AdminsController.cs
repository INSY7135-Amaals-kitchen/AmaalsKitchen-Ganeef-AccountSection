using AmaalsKitchen.Data;
using AmaalsKitchen.Models;
using AmaalsKitchen.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace AmaalsKitchen.Controllers
{
    public class AdminsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminsController(ApplicationDbContext context)
        {
            _context = context;
            Console.WriteLine("AdminsController initialized at " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        }

        [HttpGet]
        public IActionResult AddProducts()
        {
            Console.WriteLine("GET AddProducts called at " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            var vm = new ProductFormViewModel
            {
                Products = _context.Products.AsNoTracking().ToList()
            };
            Console.WriteLine($"Loaded {vm.Products.Count} products");
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddProducts(ProductFormViewModel model)
        {
            Console.WriteLine($"POST AddProducts called with Name={model?.Product?.Name}, Price={model?.Product?.Price}, Category={model?.Product?.Category}, Description={model?.Product?.Description}, ImageUrl={model?.Product?.ImageUrl} at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            if (!ModelState.IsValid)
            {
                Console.WriteLine("ModelState invalid: " + string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                model = new ProductFormViewModel { Products = _context.Products.AsNoTracking().ToList() };
                return View(model);
            }

            if (model?.Product == null)
            {
                Console.WriteLine("Model or Product is null");
                TempData["ErrorMessage"] = "Invalid form data: Product is null";
                model = new ProductFormViewModel { Products = _context.Products.AsNoTracking().ToList() };
                return View(model);
            }

            try
            {
                Console.WriteLine("Attempting to save product...");
                _context.Products.Add(model.Product);
                int rowsAffected = _context.SaveChanges();
                Console.WriteLine($"Saved {rowsAffected} row(s)");
                TempData["SuccessMessage"] = "Product added!";
            }
            catch (Exception ex)
            {
                var errorMessage = $"Failed to save: {ex.Message}";
                if (ex.InnerException != null)
                {
                    errorMessage += $" | Inner: {ex.InnerException.Message}";
                }
                Console.WriteLine(errorMessage);
                TempData["ErrorMessage"] = errorMessage;
                model.Products = _context.Products.AsNoTracking().ToList();
                return View(model);
            }

            return RedirectToAction("AddProducts");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteProduct(int id)
        {
            Console.WriteLine($"POST DeleteProduct called with Id={id} at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            if (id <= 0)
            {
                Console.WriteLine("Invalid ID: " + id);
                TempData["ErrorMessage"] = "Invalid product ID";
                return RedirectToAction("AddProducts");
            }

            try
            {
                Console.WriteLine($"Searching for product with Id={id}");
                var product = _context.Products.Find(id);
                if (product == null)
                {
                    Console.WriteLine($"Product with Id={id} not found");
                    TempData["ErrorMessage"] = $"Product with ID {id} not found";
                    return RedirectToAction("AddProducts");
                }

                Console.WriteLine($"Found product: Name={product.Name}, Category={product.Category}");
                _context.Products.Remove(product);
                Console.WriteLine("Attempting to delete product...");
                int rowsAffected = _context.SaveChanges();
                Console.WriteLine($"Deleted {rowsAffected} row(s)");
                TempData["SuccessMessage"] = $"Product '{product.Name}' deleted successfully!";
            }
            catch (Exception ex)
            {
                var errorMessage = $"Delete failed: {ex.Message}";
                if (ex.InnerException != null)
                {
                    errorMessage += $" | Inner: {ex.InnerException.Message}";
                }
                Console.WriteLine(errorMessage);
                TempData["ErrorMessage"] = errorMessage;
            }
            Console.WriteLine("Redirecting to AddProducts at " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            return RedirectToAction("AddProducts");
        }


        // ========================= DASHBOARD =========================
        public IActionResult AdminDashboard()
        {
            var orders = _context.Orders
                .Include(o => o.OrderItems)
                .Include(o => o.User)
                .ToList();

            // Revenue & Orders
            var totalRevenue = orders.Sum(o => o.Total);
            var totalOrders = orders.Count;
            var avgOrderValue = totalOrders > 0 ? orders.Average(o => o.Total) : 0;

            // Products
            var productStats = orders
                .SelectMany(o => o.OrderItems)
                .GroupBy(i => i.ItemName)
                .Select(g => new { ProductName = g.Key, Quantity = g.Sum(i => i.Quantity) })
                .OrderByDescending(g => g.Quantity)
                .ToList();

            var mostBought = productStats.FirstOrDefault();
            var top5Products = productStats.Take(5)
                .Select(p => (p.ProductName, p.Quantity))
                .ToList();

            // Customers
            var customerStats = orders
                .GroupBy(o => o.User != null ? o.User.FirstName + " " + o.User.LastName : "Unknown")
                .Select(g => new { Customer = g.Key, Spend = g.Sum(o => o.Total) })
                .OrderByDescending(c => c.Spend)
                .ToList();

            var topCustomer = customerStats.FirstOrDefault();

            var model = new DashboardViewModel
            {
                TotalRevenue = totalRevenue,
                TotalOrders = totalOrders,
                AverageOrderValue = avgOrderValue,
                MostBoughtItem = mostBought?.ProductName ?? "No sales yet",
                MostBoughtCount = mostBought?.Quantity ?? 0,
                Top5Products = top5Products,
                TotalCustomers = customerStats.Count,
                TopCustomer = topCustomer?.Customer ?? "N/A",
                TopCustomerSpend = topCustomer?.Spend ?? 0
            };

            return View(model);
        }
    }
}