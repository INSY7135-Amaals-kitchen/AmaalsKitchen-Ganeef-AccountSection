// Services/OrderService.cs
using AmaalsKitchen.Models;
using AmaalsKitchen.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AmaalsKitchen.Services
{
    public class OrderService : IOrderService
    {
        private readonly ApplicationDbContext _context;

        public OrderService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Order> CreateOrderAsync(Order order)
        {
            try
            {
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();
                return order;
            }
            catch (DbUpdateException ex)
            {
                // Log the detailed error
                Console.WriteLine($"Database error: {ex.InnerException?.Message ?? ex.Message}");
                throw new Exception("Failed to save order to database. Please try again.");
            }
        }

        public async Task<List<Order>> GetAllOrdersAsync()
        {
            try
            {
                return await _context.Orders
                    .Include(o => o.OrderItems)
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving orders: {ex.Message}");
                return new List<Order>();
            }
        }

        public async Task<Order> GetOrderByIdAsync(int id)
        {
            try
            {
                return await _context.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.Id == id);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving order {id}: {ex.Message}");
                return null;
            }
        }

        public async Task UpdateOrderStatusAsync(int orderId, OrderStatus status)
        {
            try
            {
                var order = await _context.Orders.FindAsync(orderId);
                if (order != null)
                {
                    order.Status = status;
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating order status: {ex.Message}");
                throw;
            }
        }
    }
}