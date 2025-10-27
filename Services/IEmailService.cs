namespace AmaalsKitchen.Services
{
    public interface IEmailService
    {
        Task<bool> SendOrderConfirmationAsync(string toEmail, string customerName, int orderId, decimal total);
        Task<bool> SendOrderStatusUpdateAsync(string toEmail, string customerName, int orderId, string status);
    }
}