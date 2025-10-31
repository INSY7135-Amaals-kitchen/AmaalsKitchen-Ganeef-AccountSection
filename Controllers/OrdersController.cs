/// =============================================================================
/// FILE: OrdersController.cs
/// PURPOSE: Handles all order-related operations including cart management,
///          checkout, order placement, and order history viewing.
/// SECURITY: Validates user sessions and restricts admin users from placing orders.
/// BUSINESS LOGIC: 
///   - Cart stored in session as JSON
///   - Tax calculated at 15% of subtotal
///   - Preparation time: 15 min base + 5 min per additional item (max 45 min)
///   - Email notifications sent on order placement and status changes
/// =============================================================================
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

        /// <summary>
        /// Initializes OrdersController with required services.
        /// </summary>
        /// <param name="orderService">Service for order database operations</param>
        /// <param name="context">Database context for user and order data</param>
        /// <param name="emailService">Service for sending notification emails</param>
        public OrdersController(IOrderService orderService, ApplicationDbContext context, IEmailService emailService)
        {
            _orderService = orderService;
            _context = context;
            _emailService = emailService;
        }

        /// <summary>
        /// Displays checkout page with current cart contents.
        /// SECURITY: Requires user login. Prevents admins from placing orders.
        /// VALIDATION: Checks for empty cart before allowing checkout.
        /// </summary>
        /// <returns>Checkout view with cart data, or redirects to login/dashboard/menu</returns>
        public IActionResult Checkout()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            var userRole = HttpContext.Session.GetString("UserRole");

            // Ensure user is logged in
            if (string.IsNullOrEmpty(userEmail))
            {
                TempData["ErrorMessage"] = "Please log in to proceed to checkout.";
                return RedirectToAction("Login", "Account");
            }

            // Prevent admins from placing orders (business rule)
            if (userRole == "Admin")
            {
                TempData["ErrorMessage"] = "Admins are not allowed to place orders.";
                return RedirectToAction("AdminDashboard", "Admins");
            }

            var cart = GetCartFromSession();

            // Validate cart has items
            if (!cart.Items.Any())
            {
                TempData["ErrorMessage"] = "Your cart is empty. Please add items before checkout.";
                return RedirectToAction("Menu", "Home");
            }

            return View(cart);
        }

        /// <summary>
        /// Processes order placement and sends confirmation email.
        /// CALCULATION: Subtotal + 15% Tax = Total
        /// PREPARATION TIME: Calculated dynamically based on item count
        /// EMAIL: Sends confirmation email asynchronously (failure doesn't block order)
        /// SESSION: Clears cart after successful order placement
        /// </summary>
        /// <param name="notes">Optional customer notes/special instructions</param>
        /// <returns>JSON response with order details and estimated pickup time</returns>
        [HttpPost]
        public async Task<IActionResult> PlaceOrder(string notes = "")
        {
            try
            {
                var cart = GetCartFromSession();

                if (!cart.Items.Any())
                    return Json(new { success = false, message = "Cart is empty" });

                // Get current user from database
                var userEmail = HttpContext.Session.GetString("UserEmail");
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
                int? userId = user?.Id;

                // Calculate preparation time based on number of items
                int prepTime = CalculatePreparationTime(cart.Items.Count);

                // Create order object with all details
                var order = new Order
                {
                    Subtotal = cart.Subtotal,
                    Tax = cart.Tax,  // 15% of subtotal
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

                // Save order to database
                var createdOrder = await _orderService.CreateOrderAsync(order);

                // Send order confirmation email (non-blocking)
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
                        // Email failure shouldn't stop order placement
                        Console.WriteLine($"Email failed: {ex.Message}");
                    }
                }

                // Clear cart after successful order
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

        /// <summary>
        /// Calculates dynamic preparation time based on order complexity.
        /// ALGORITHM: 
        /// - Base time: 15 minutes
        /// - Each additional item: +5 minutes
        /// - Maximum cap: 45 minutes
        /// EXAMPLE: 1 item = 15 min, 3 items = 25 min, 7+ items = 45 min
        /// </summary>
        /// <param name="itemCount">Total number of items in order</param>
        /// <returns>Preparation time in minutes</returns>
        private int CalculatePreparationTime(int itemCount)
        {
            int baseTime = 15;
            int additionalTime = Math.Max(0, (itemCount - 1) * 5);
            return Math.Min(baseTime + additionalTime, 45);
        }

        /// <summary>
        /// Displays all orders for the currently logged-in user.
        /// Orders are sorted by date (newest first).
        /// SECURITY: Requires active user session.
        /// </summary>
        /// <returns>View with user's order history</returns>
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

                // Retrieve user orders with related items (eager loading)
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

        /// <summary>
        /// Admin view: Shows all orders with optional filtering.
        /// FILTERS: 
        /// - Today: Orders from current date
        /// - Last 10 Days: Rolling 10-day window
        /// - Specific Date: User-selected date
        /// SECURITY: Intended for admin use only (should add [Authorize] in production)
        /// </summary>
        /// <param name="filterType">Type of date filter to apply (today/last10days/specific)</param>
        /// <param name="specificDate">Specific date for filtering (optional)</param>
        /// <returns>View with filtered order list</returns>
        public async Task<IActionResult> AllOrders(string filterType, DateTime? specificDate)
        {
            try
            {
                var ordersQuery = _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.OrderItems)
                    .AsQueryable();

                // Apply date filters based on selection
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

        /// <summary>
        /// Updates order status (Admin only).
        /// Sends email notification when status changes to Preparing or ReadyForPickup.
        /// Sets actual pickup time when status changes to Completed.
        /// SECURITY: Validates admin role before allowing status change.
        /// EMAIL: Sends appropriate notification based on new status.
        /// </summary>
        /// <param name="orderId">Order ID to update</param>
        /// <param name="newStatus">New status to set</param>
        /// <returns>JSON response with success status and updated order info</returns>
        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, OrderStatus newStatus)
        {
            try
            {
                // Security check: Only admins can update order status
                var userRole = HttpContext.Session.GetString("UserRole");
                if (userRole != "Admin")
                    return Json(new { success = false, message = "Unauthorized" });

                var order = await _context.Orders.Include(o => o.User).FirstOrDefaultAsync(o => o.Id == orderId);
                if (order == null)
                    return Json(new { success = false, message = "Order not found" });

                order.Status = newStatus;

                // Record actual pickup time when order is completed
                if (newStatus == OrderStatus.Completed)
                    order.ActualPickupTime = DateTime.Now;

                await _context.SaveChangesAsync();

                // Send status update email for key status changes
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

        /// <summary>
        /// Displays detailed information for a specific order.
        /// SECURITY: Users can only view their own orders unless they are admin.
        /// INCLUDES: Order items, pricing breakdown, status, and timing information.
        /// </summary>
        /// <param name="id">Order ID</param>
        /// <returns>Order details view or redirect with error</returns>
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

                // Authorization check: Must be admin or order owner
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

        // ==================== CART MANAGEMENT ====================

        /// <summary>
        /// AJAX endpoint: Updates quantity of a specific cart item.
        /// If quantity is 0 or less, removes item from cart.
        /// SESSION: Updates cart in session storage.
        /// </summary>
        /// <param name="itemId">Cart item ID</param>
        /// <param name="quantity">New quantity (0 or less removes item)</param>
        /// <returns>JSON with updated cart totals or redirect to checkout</returns>
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

                // Return updated totals for AJAX requests
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

        /// <summary>
        /// AJAX endpoint: Removes a specific item from cart.
        /// SESSION: Updates cart in session storage.
        /// </summary>
        /// <param name="itemId">Cart item ID to remove</param>
        /// <returns>JSON with updated cart totals or redirect to checkout</returns>
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

        /// <summary>
        /// Clears all items from cart by removing session data.
        /// </summary>
        /// <returns>JSON response indicating success or failure</returns>
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

        /// <summary>
        /// Adds a product to the cart or increments quantity if already exists.
        /// SESSION STORAGE: Cart is stored as JSON in session state.
        /// LOGIC: If item already in cart, increment quantity; otherwise add new item.
        /// </summary>
        /// <param name="id">Product ID</param>
        /// <param name="name">Product name</param>
        /// <param name="price">Product price</param>
        /// <param name="imageUrl">Product image URL (optional)</param>
        /// <returns>JSON response with updated cart totals and item list</returns>
        [HttpPost]
        public IActionResult AddToCart(int id, string name, decimal price, string imageUrl = "")
        {
            try
            {
                var cart = GetCartFromSession();
                var existingItem = cart.Items.FirstOrDefault(i => i.Id == id && i.Name == name);

                if (existingItem != null)
                    existingItem.Quantity += 1;  // Increment if already in cart
                else
                {
                    // Add new item to cart
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

        // ==================== HELPER METHODS ====================

        /// <summary>
        /// Retrieves cart from session storage (deserializes JSON).
        /// Returns empty cart if none exists in session.
        /// SESSION KEY: "Cart"
        /// </summary>
        /// <returns>Cart object with items or empty cart</returns>
        private Cart GetCartFromSession()
        {
            var cartJson = HttpContext.Session.GetString("Cart");
            return string.IsNullOrEmpty(cartJson)
                ? new Cart()
                : JsonConvert.DeserializeObject<Cart>(cartJson);
        }

        /// <summary>
        /// Saves cart to session storage (serializes to JSON).
        /// SESSION KEY: "Cart"
        /// </summary>
        /// <param name="cart">Cart object to save</param>
        private void SaveCartToSession(Cart cart)
        {
            var cartJson = JsonConvert.SerializeObject(cart);
            HttpContext.Session.SetString("Cart", cartJson);
        }

        /// <summary>
        /// Updates the cart item counter in ViewBag for display in navigation.
        /// Used to update the badge showing number of items in cart.
        /// </summary>
        private void UpdateCartCounter()
        {
            var cart = GetCartFromSession();
            ViewBag.CartItemCount = cart.TotalItems;
        }
    }
}