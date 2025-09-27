using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AmaalsKitchen.Models
{
    public class Order
    {
        public int Id { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.Now;
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public decimal Subtotal { get; set; }
        public decimal Tax { get; set; }
        public decimal Total { get; set; }
        public int? UserId { get; set; }

        // Navigation property
        public User User { get; set; }

        public List<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }

    public class OrderItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }

        [Required]
        public string ItemName { get; set; }

        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string ImageUrl { get; set; }

        // Navigation property
        public Order Order { get; set; }
    }

    public enum OrderStatus
    {
        Pending,
        Completed,
        Cancelled
    }
}