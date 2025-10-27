using System.Collections.Generic;

namespace AmaalsKitchen.Models
{
    public class DashboardViewModel
    {
        // Revenue & Orders
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public decimal AverageOrderValue { get; set; }

        // Products
        public string MostBoughtItem { get; set; }
        public int MostBoughtCount { get; set; }
        public List<(string ProductName, int Quantity)> Top5Products { get; set; }

        // Customers
        public int TotalCustomers { get; set; }
        public string TopCustomer { get; set; }
        public decimal TopCustomerSpend { get; set; }
    }
}
