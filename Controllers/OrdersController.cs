using AmaalsKitchen.Data;
using AmaalsKitchen.Models;
using AmaalsKitchen.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AmaalsKitchen.Controllers
{
    public class OrdersController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public OrdersController(IOrderService orderService, ApplicationDbContext context, IEmailService emailService)
        {
            _orderService = orderService;
            _context = context;
            _emailService = emailService;
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

            if (!cart.Items.Any())
            {
                TempData["ErrorMessage"] = "Your cart is empty. Please add items before checkout.";
                return RedirectToAction("Menu", "Home");
            }

            return View(cart);
        }

        [HttpPost]
        public async Task<IActionResult> PlaceOrder(string notes = "")
        {
            try
            {
                var cart = GetCartFromSession();

                if (!cart.Items.Any())
                    return Json(new { success = false, message = "Cart is empty" });

                var userEmail = HttpContext.Session.GetString("UserEmail");
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
                int? userId = user?.Id;

                int prepTime = CalculatePreparationTime(cart.Items.Count);

                var order = new Order
                {
                    Subtotal = cart.Subtotal,
                    Tax = cart.Tax,
                    Total = cart.Total,
                    UserId = userId,
                    OrderDate = DateTime.Now,
                    Status = OrderStatus.Pending,
                    PreparationTimeMinutes = prepTime,
                    EstimatedPickupTime = DateTime.Now.AddMinutes(prepTime),
                    Notes = notes,
                    OrderItems = cart.Items.Select(item => new OrderItem
                    {
                        ItemName = item.Name,
                        Price = item.Price,
                        Quantity = item.Quantity,
                        ImageUrl = item.ImageUrl
                    }).ToList()
                };

                var createdOrder = await _orderService.CreateOrderAsync(order);

                // ✅ Send confirmation email
                if (user != null)
                {
                    try
                    {
                        await _emailService.SendOrderConfirmationAsync(
                            user.Email,
                            user.FirstName,
                            createdOrder.Id,
                            createdOrder.Total
                        );
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Email failed: {ex.Message}");
                    }
                }

                HttpContext.Session.Remove("Cart");
                UpdateCartCounter();

                return Json(new
                {
                    success = true,
                    message = "Order placed successfully!",
                    orderId = createdOrder.Id,
                    estimatedPickupTime = createdOrder.EstimatedPickupTime.ToString("hh:mm tt"),
                    preparationTime = prepTime
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error placing order: {ex.Message}");
                return Json(new { success = false, message = "Error placing order. Please try again." });
            }
        }

        private int CalculatePreparationTime(int itemCount)
        {
            int baseTime = 15;
            int additionalTime = Math.Max(0, (itemCount - 1) * 5);
            return Math.Min(baseTime + additionalTime, 45);
        }

        public async Task<IActionResult> MyOrders()
        {
            try
            {
                var userEmail = HttpContext.Session.GetString("UserEmail");
                if (string.IsNullOrEmpty(userEmail))
                {
                    TempData["ErrorMessage"] = "Please log in to view your orders.";
                    return RedirectToAction("Login", "Account");
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToAction("Login", "Account");
                }

                var orders = await _context.Orders
                    .Include(o => o.OrderItems)
                    .Where(o => o.UserId == user.Id)
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();

                return View(orders);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving orders: {ex.Message}");
                return View(new List<Order>());
            }
        }

        public async Task<IActionResult> AllOrders(string filterType, DateTime? specificDate)
        {
            try
            {
                var ordersQuery = _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.OrderItems)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(filterType))
                {
                    switch (filterType.ToLower())
                    {
                        case "today":
                            ordersQuery = ordersQuery.Where(o => o.OrderDate.Date == DateTime.Today);
                            break;
                        case "last10days":
                            var fromDate = DateTime.Today.AddDays(-10);
                            ordersQuery = ordersQuery.Where(o => o.OrderDate.Date >= fromDate && o.OrderDate.Date <= DateTime.Today);
                            break;
                        case "specific":
                            if (specificDate.HasValue)
                                ordersQuery = ordersQuery.Where(o => o.OrderDate.Date == specificDate.Value.Date);
                            break;
                    }
                }

                var orders = await ordersQuery.OrderByDescending(o => o.OrderDate).ToListAsync();
                return View(orders);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving all orders: {ex.Message}");
                return View(new List<Order>());
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, OrderStatus newStatus)
        {
            try
            {
                var userRole = HttpContext.Session.GetString("UserRole");
                if (userRole != "Admin")
                    return Json(new { success = false, message = "Unauthorized" });

                var order = await _context.Orders.Include(o => o.User).FirstOrDefaultAsync(o => o.Id == orderId);
                if (order == null)
                    return Json(new { success = false, message = "Order not found" });

                order.Status = newStatus;

                if (newStatus == OrderStatus.Completed)
                    order.ActualPickupTime = DateTime.Now;

                await _context.SaveChangesAsync();

                // ✅ Send email on status change
                if (order.User != null && (newStatus == OrderStatus.Preparing || newStatus == OrderStatus.ReadyForPickup))
                {
                    try
                    {
                        await _emailService.SendOrderStatusUpdateAsync(
                            order.User.Email,
                            order.User.FirstName,
                            order.Id,
                            newStatus.ToString()
                        );
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Email failed: {ex.Message}");
                    }
                }

                return Json(new
                {
                    success = true,
                    message = $"Order #{orderId} status updated to {newStatus}",
                    newStatus = order.StatusDisplay
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating order status: {ex.Message}");
                return Json(new { success = false, message = "Error updating status" });
            }
        }

        public async Task<IActionResult> OrderDetails(int id)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order == null)
                {
                    TempData["ErrorMessage"] = "Order not found.";
                    return RedirectToAction("MyOrders");
                }

                var userEmail = HttpContext.Session.GetString("UserEmail");
                var userRole = HttpContext.Session.GetString("UserRole");

                if (userRole != "Admin" && order.User?.Email != userEmail)
                {
                    TempData["ErrorMessage"] = "You don't have permission to view this order.";
                    return RedirectToAction("MyOrders");
                }

                return View(order);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving order details: {ex.Message}");
                TempData["ErrorMessage"] = "Error loading order details.";
                return RedirectToAction("MyOrders");
            }
        }

        [HttpPost]
        public IActionResult UpdateQuantity(int itemId, int quantity)
        {
            var cart = GetCartFromSession();
            var item = cart.Items.FirstOrDefault(i => i.Id == itemId);

            if (item != null)
            {
                if (quantity <= 0)
                    cart.Items.Remove(item);
                else
                    item.Quantity = quantity;

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
                    existingItem.Quantity += 1;
                else
                {
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
                    items = cart.Items
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
