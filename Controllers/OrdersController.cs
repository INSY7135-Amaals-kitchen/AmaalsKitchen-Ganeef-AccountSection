using AmaalsKitchen.Data;
using AmaalsKitchen.Models;
using AmaalsKitchen.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AmaalsKitchen.Controllers
{
    public class OrdersController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly ApplicationDbContext _context;
        public OrdersController(IOrderService orderService, ApplicationDbContext context)
        {
            _orderService = orderService;
            _context = context;
        }

        public IActionResult Checkout()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            var userRole = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrEmpty(userEmail))
            {
                TempData["ErrorMessage"] = "Please log in to proceed to checkout.";
                return RedirectToAction("Login", "Account"); 
            }
            if (userRole == "Admin")
            {
                TempData["ErrorMessage"] = "Admins are not allowed to place orders.";
                return RedirectToAction("AdminDashboard", "Admins"); 
            }

            var cart = GetCartFromSession();
            return View(cart);
        }

        [HttpPost]
        public async Task<IActionResult> PlaceOrder()
        {
            try
            {
                var cart = GetCartFromSession();

                if (!cart.Items.Any())
                {
                    return Json(new { success = false, message = "Cart is empty" });
                }

                // Get the logged-in user's email from session
                var userEmail = HttpContext.Session.GetString("UserEmail");
                int? userId = null;

                if (!string.IsNullOrEmpty(userEmail))
                {
                    // Assuming you have a DbContext _context for Users
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
                    if (user != null)
                    {
                        userId = user.Id;
                    }
                }

                // Create new order with the cart items
                var order = new Order
                {
                    Subtotal = cart.Subtotal,
                    Tax = cart.Tax,
                    Total = cart.Total,
                    UserId = userId, // <-- assign the logged-in user's ID
                    OrderItems = cart.Items.Select(item => new OrderItem
                    {
                        ItemName = item.Name,
                        Price = item.Price,
                        Quantity = item.Quantity,
                        ImageUrl = item.ImageUrl
                    }).ToList()
                };

                var createdOrder = await _orderService.CreateOrderAsync(order);

                // Clear cart after successful order
                HttpContext.Session.Remove("Cart");
                UpdateCartCounter();

                return Json(new
                {
                    success = true,
                    message = "Order placed successfully!",
                    orderId = createdOrder.Id
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error placing order: {ex.Message}");
                return Json(new
                {
                    success = false,
                    message = "Error placing order. Please try again."
                });
            }
        }


        public async Task<IActionResult> MyOrders()
        {
            try
            {
                // Get the logged-in user's email from session
                var userEmail = HttpContext.Session.GetString("UserEmail");
                if (string.IsNullOrEmpty(userEmail))
                {
                    TempData["ErrorMessage"] = "Please log in to view your orders.";
                    return RedirectToAction("Login", "Account");
                }

                // Find the user's ID
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToAction("Login", "Account");
                }

                // Get only orders for this user
                var orders = await _context.Orders
                                           .Include(o => o.OrderItems)
                                           .Where(o => o.UserId == user.Id)
                                           .ToListAsync();

                return View(orders);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving orders: {ex.Message}");
                return View(new List<Order>());
            }
        }
        public async Task<IActionResult> AllOrders()
        {
            try
            {
                // Get all orders and include User for display purposes
                var orders = await _context.Orders
                    .Include(o => o.User) // Include related User info
                    .Include(o => o.OrderItems) // Include items in the order
                    .OrderByDescending(o => o.OrderDate) // Optional: newest first
                    .ToListAsync();

                return View(orders); // Pass to view
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving all orders: {ex.Message}");
                return View(new List<Order>());
            }
        }

        // ... Keep the rest of your existing methods (UpdateQuantity, RemoveItem, etc.)

        [HttpPost]
        public IActionResult UpdateQuantity(int itemId, int quantity)
        {
            var cart = GetCartFromSession();
            var item = cart.Items.FirstOrDefault(i => i.Id == itemId);

            if (item != null)
            {
                if (quantity <= 0)
                {
                    cart.Items.Remove(item);
                }
                else
                {
                    item.Quantity = quantity;
                }

                SaveCartToSession(cart);
                UpdateCartCounter();

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new
                    {
                        success = true,
                        totalItems = cart.TotalItems,
                        subtotal = cart.Subtotal,
                        tax = cart.Tax,
                        total = cart.Total
                    });
                }
            }

            return RedirectToAction("Checkout");
        }

        [HttpPost]
        public IActionResult RemoveItem(int itemId)
        {
            var cart = GetCartFromSession();
            var item = cart.Items.FirstOrDefault(i => i.Id == itemId);

            if (item != null)
            {
                cart.Items.Remove(item);
                SaveCartToSession(cart);
                UpdateCartCounter();

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new
                    {
                        success = true,
                        totalItems = cart.TotalItems,
                        subtotal = cart.Subtotal,
                        tax = cart.Tax,
                        total = cart.Total
                    });
                }
            }

            return RedirectToAction("Checkout");
        }

        [HttpPost]
        public IActionResult ClearCart()
        {
            try
            {
                HttpContext.Session.Remove("Cart");
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error clearing cart: {ex.Message}");
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult AddToCart(int id, string name, decimal price, string imageUrl = "")
        {
            try
            {
                var cart = GetCartFromSession();
                var existingItem = cart.Items.FirstOrDefault(i => i.Id == id && i.Name == name);

                if (existingItem != null)
                {
                    // Item already exists, increase quantity
                    existingItem.Quantity += 1;
                }
                else
                {
                    // Add new item
                    cart.Items.Add(new CartItem
                    {
                        Id = id,
                        Name = name,
                        Price = price,
                        Quantity = 1,
                        ImageUrl = imageUrl
                    });
                }

                SaveCartToSession(cart);
                UpdateCartCounter();

                return Json(new
                {
                    success = true,
                    totalItems = cart.TotalItems,
                    cartItemCount = cart.Items.Count,
                    items = cart.Items // Return items for debugging
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in AddToCart: {ex.Message}");
                return Json(new { success = false, error = ex.Message });
            }
        }

        private Cart GetCartFromSession()
        {
            var cartJson = HttpContext.Session.GetString("Cart");
            return string.IsNullOrEmpty(cartJson)
                ? new Cart()
                : JsonConvert.DeserializeObject<Cart>(cartJson);
        }

        private void SaveCartToSession(Cart cart)
        {
            var cartJson = JsonConvert.SerializeObject(cart);
            HttpContext.Session.SetString("Cart", cartJson);
        }

        private void UpdateCartCounter()
        {
            var cart = GetCartFromSession();
            ViewBag.CartItemCount = cart.TotalItems;
        }
    }
}