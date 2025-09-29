using AmaalsKitchen.Models;

namespace AmaalsKitchen.ViewModels
{
    public class ProductFormViewModel
    {
        public Product Product { get; set; } = new Product();
        public List<Product> Products { get; set; } = new List<Product>();
    }
}
