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

        // NEW: Estimated pickup time
        public DateTime EstimatedPickupTime { get; set; }

        // NEW: Actual pickup time (when order is completed)
        public DateTime? ActualPickupTime { get; set; }

        // NEW: Preparation time in minutes
        public int PreparationTimeMinutes { get; set; } = 25;

        // NEW: Order notes/special instructions
        public string Notes { get; set; }

        public decimal Subtotal { get; set; }
        public decimal Tax { get; set; }
        public decimal Total { get; set; }
        public int? UserId { get; set; }

        // Navigation property
        public User User { get; set; }

        public List<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

        // Helper property to display status with color
        public string StatusDisplay => Status switch
        {
            OrderStatus.Pending => "Pending",
            OrderStatus.Preparing => "Being Prepared",
            OrderStatus.ReadyForPickup => "Ready for Pickup",
            OrderStatus.Completed => "Completed",
            OrderStatus.Cancelled => "Cancelled",
            _ => "Unknown"
        };

        // Helper to check if order can be picked up
        public bool IsReadyForPickup => Status == OrderStatus.ReadyForPickup;

        // Helper to get time remaining until pickup
        public string TimeUntilPickup
        {
            get
            {
                if (Status == OrderStatus.Completed || Status == OrderStatus.Cancelled)
                    return "N/A";

                var remaining = EstimatedPickupTime - DateTime.Now;
                if (remaining.TotalMinutes < 0)
                    return "Ready Now!";

                return $"{(int)remaining.TotalMinutes} minutes";
            }
        }
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
        Pending = 0,           // Order just placed
        Preparing = 1,         // Kitchen is preparing
        ReadyForPickup = 2,    // Ready to be collected
        Completed = 3,         // Customer has collected
        Cancelled = 4          // Order cancelled
    }
}