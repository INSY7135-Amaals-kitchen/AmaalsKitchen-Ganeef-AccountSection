/// =============================================================================
/// FILE: EmailService.cs
/// PURPOSE: Handles email notifications for order confirmations and status updates.
/// TECHNOLOGY: Uses MailKit library with Gmail SMTP (port 587, STARTTLS)
/// CONFIGURATION: Requires appsettings.json EmailSettings section
/// =============================================================================
/// 
using AmaalsKitchen.Services;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System.Threading.Tasks;

namespace AmaalsKitchen.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _senderEmail;
        private readonly string _senderName;
        private readonly string _password;
        private readonly bool _emailEnabled;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
            _smtpServer = _configuration["EmailSettings:SmtpServer"];
            _smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"]);
            _senderEmail = _configuration["EmailSettings:SenderEmail"];
            _senderName = _configuration["EmailSettings:SenderName"];
            _password = _configuration["EmailSettings:Password"];
            _emailEnabled = bool.Parse(_configuration["EmailSettings:EmailEnabled"] ?? "true");
        }

        public async Task<bool> SendOrderConfirmationAsync(string toEmail, string customerName, int orderId, decimal total)
        {
            if (!_emailEnabled) return false;

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_senderName, _senderEmail));
                message.To.Add(new MailboxAddress(customerName, toEmail));
                message.Subject = $"Order Confirmation - #{orderId}";

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = $@"
                        <html>
                        <body style='font-family: Arial, sans-serif;'>
                            <div style='background-color: #C41E3A; color: white; padding: 20px; text-align: center;'>
                                <h1>Thank You for Your Order!</h1>
                            </div>
                            <div style='padding: 20px;'>
                                <p>Hi {customerName},</p>
                                <p>We've received your order and are getting it ready!</p>
                                <div style='background-color: #FFF8E1; padding: 15px; border-left: 4px solid #FDB913; margin: 20px 0;'>
                                    <h3>Order Details</h3>
                                    <p><strong>Order Number:</strong> #{orderId}</p>
                                    <p><strong>Total:</strong> R{total:F2}</p>
                                    <p><strong>Estimated Ready Time:</strong> 25 minutes</p>
                                </div>
                                <p>You'll receive another email when your order is ready for collection.</p>
                                <p>Thank you for choosing Amaals Kitchen!</p>
                                <hr style='margin: 30px 0;'/>
                                <p style='color: #666; font-size: 12px;'>
                                    Amaals Kitchen | 123 Main Street, Cape Town<br/>
                                    Phone: +27 12 345 6789
                                </p>
                            </div>
                        </body>
                        </html>"
                };

                message.Body = bodyBuilder.ToMessageBody();

                using (var client = new SmtpClient())
                {
                    // ✅ Use STARTTLS for Gmail on port 587
                    await client.ConnectAsync(_smtpServer, _smtpPort, SecureSocketOptions.StartTls);
                    await client.AuthenticateAsync(_senderEmail, _password);
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }

                Console.WriteLine($"✅ Order confirmation email sent to {toEmail}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error sending email: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendOrderStatusUpdateAsync(string toEmail, string customerName, int orderId, string status)
        {
            if (!_emailEnabled) return false;

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_senderName, _senderEmail));
                message.To.Add(new MailboxAddress(customerName, toEmail));

                string subject = "";
                string htmlBody = "";

                switch (status)
                {
                    case "Preparing":
                        subject = $"Your Order is Being Prepared - #{orderId}";
                        htmlBody = $@"
                            <html>
                            <body style='font-family: Arial, sans-serif;'>
                                <div style='background-color: #17a2b8; color: white; padding: 20px; text-align: center;'>
                                    <h1>🍽️ Order Being Prepared</h1>
                                </div>
                                <div style='padding: 20px;'>
                                    <p>Hi {customerName},</p>
                                    <p>Great news! Your order #{orderId} is now being prepared.</p>
                                    <p><strong>Estimated ready time: 25 minutes</strong></p>
                                    <p>We'll notify you when it's ready for collection.</p>
                                    <p>- Amaals Kitchen Team</p>
                                </div>
                            </body>
                            </html>";
                        break;

                    case "Ready":
                        subject = $"✅ Your Order is Ready! - #{orderId}";
                        htmlBody = $@"
                            <html>
                            <body style='font-family: Arial, sans-serif;'>
                                <div style='background-color: #28a745; color: white; padding: 20px; text-align: center;'>
                                    <h1>✅ Order Ready for Collection!</h1>
                                </div>
                                <div style='padding: 20px;'>
                                    <p>Hi {customerName},</p>
                                    <p><strong>Your order #{orderId} is READY for collection!</strong></p>
                                    <div style='background-color: #d1e7dd; padding: 15px; border-left: 4px solid #28a745; margin: 20px 0;'>
                                        <p><strong>📍 Collection Address:</strong><br/>
                                        123 Main Street, Cape Town</p>
                                        <p><strong>⏰ Please collect at your earliest convenience</strong></p>
                                    </div>
                                    <p>We look forward to seeing you!</p>
                                    <p>- Amaals Kitchen Team</p>
                                </div>
                            </body>
                            </html>";
                        break;

                    default:
                        subject = $"Order Update - #{orderId}";
                        htmlBody = $@"
                            <html>
                            <body style='font-family: Arial, sans-serif;'>
                                <div style='padding: 20px;'>
                                    <p>Hi {customerName},</p>
                                    <p>Your order #{orderId} status: <strong>{status}</strong></p>
                                    <p>- Amaals Kitchen Team</p>
                                </div>
                            </body>
                            </html>";
                        break;
                }

                message.Subject = subject;
                var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
                message.Body = bodyBuilder.ToMessageBody();

                using (var client = new SmtpClient())
                {
                    await client.ConnectAsync(_smtpServer, _smtpPort, SecureSocketOptions.StartTls);
                    await client.AuthenticateAsync(_senderEmail, _password);
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }

                Console.WriteLine($"✅ Status update email sent to {toEmail}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error sending status email: {ex.Message}");
                return false;
            }
        }
    }
}
