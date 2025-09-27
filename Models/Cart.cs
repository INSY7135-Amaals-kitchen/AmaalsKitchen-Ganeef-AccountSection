// Models/Cart.cs
using System.Collections.Generic;
using System.Linq;

namespace AmaalsKitchen.Models
{
    public class Cart
    {
        public List<CartItem> Items { get; set; } = new List<CartItem>();

        public int TotalItems => Items.Sum(i => i.Quantity);
        public decimal Subtotal => Items.Sum(i => i.Price * i.Quantity);
        public decimal Tax => Subtotal * 0.15m;
        public decimal Total => Subtotal + Tax;

        public void AddItem(CartItem item)
        {
            var existingItem = Items.FirstOrDefault(i => i.Id == item.Id && i.Name == item.Name);
            if (existingItem != null)
            {
                existingItem.Quantity += item.Quantity;
            }
            else
            {
                Items.Add(item);
            }
        }

        public void RemoveItem(int itemId)
        {
            var item = Items.FirstOrDefault(i => i.Id == itemId);
            if (item != null)
            {
                Items.Remove(item);
            }
        }

        public void UpdateQuantity(int itemId, int quantity)
        {
            var item = Items.FirstOrDefault(i => i.Id == itemId);
            if (item != null)
            {
                if (quantity <= 0)
                {
                    Items.Remove(item);
                }
                else
                {
                    item.Quantity = quantity;
                }
            }
        }
    }

    public class CartItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string ImageUrl { get; set; }
    }
}