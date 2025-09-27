using AmaalsKitchen.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AmaalsKitchen.Services
{
    public interface IOrderService
    {
        Task<Order> CreateOrderAsync(Order order);
        Task<List<Order>> GetAllOrdersAsync();
        Task<Order> GetOrderByIdAsync(int id);
        Task UpdateOrderStatusAsync(int orderId, OrderStatus status);
    }
}