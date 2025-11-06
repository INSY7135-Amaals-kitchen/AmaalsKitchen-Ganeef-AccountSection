# Amaals Kitchen - Web-Based Ordering System

## Project Overview
Amaals Kitchen is a web-based ordering system developed for a local takeaway business in Cape Town, South Africa. The system allows customers to browse the menu, place collection-only orders, and track their order status in real-time. Admins can manage products, process orders, and view business analytics through a comprehensive dashboard.

**Developed by:** SimplyTech Solutions  
**Client:** Amaals Kitchen (Natheer - Owner)  
**Project Duration:** 8 weeks (September - November 2024)  
**Presentation Date:** November 4, 2025

---

## Team Members

- **Ilyaas Kamish** (ST10391174) - Project Leader & Backend Developer
- **Mogammad Ganief Salie** (ST10214012) -  UI/UX Design & Client Liaison/backend+frontend
- **Khenende Netshivhambe** (ST10379469) - QA & Testing/backend/frontend
- **Onello Travis Tarjanne** (ST10178800) - Technical Documentation/backend/frontend
- **Liyema Mangcu** (ST10143385) -Frontend Developer/backend

---

## Features

### Customer Features
- User registration and secure login
- Browse menu with category filtering (Rolls, Burgers, Drinks)
- Shopping cart with real-time total calculations
- Place orders with special instructions
- View order history and track order status
- Real-time order status updates (Pending → Preparing → Ready → Completed)
- Email notifications for order confirmation and status updates
- Profile management

### Admin Features
- Secure admin authentication
- Comprehensive analytics dashboard
  - Total revenue and order statistics
  - Top-selling products
  - Customer insights
- Product management (Add/Delete menu items)
- Order management with status workflow
- View all customer orders with filtering options
- Real-time order status updates

---

## Technology Stack

### Frontend
- HTML5, CSS3, JavaScript
- Bootstrap 5 (Responsive Design)
- jQuery for AJAX operations
- Razor Views (ASP.NET Core)

### Backend
- ASP.NET Core 8.0 MVC
- C# Programming Language
- Entity Framework Core (ORM)
- Session-based Authentication

### Database
- Microsoft SQL Server
- Entity Framework Code-First Migrations

### Tools & Services
- Visual Studio 2022
- GitHub (Version Control)
- MailKit (Email Notifications)
- Microsoft Project (Project Management)

---

## Installation & Setup

### Prerequisites
- .NET 8.0 SDK
- SQL Server 2019 or later (Express Edition is sufficient)
- Visual Studio 2022 or later
- Git

### Step 1: Clone the Repository
```bash
git clone https://github.com/INSY7135-Amaals-kitchen/AmaalsKitchen-Ganeef-AccountSection.git
cd AmaalsKitchen-Ganeef-AccountSection
```
### Step 2: Configure Database Connection
1. Open `appsettings.json`
2. Update the connection string with your SQL Server details:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=YOUR_SERVER_NAME;Database=AmaalsKitchenDB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
}
```

### Step 3: Run Database Migrations
Open Package Manager Console in Visual Studio and run:
```bash
Add-Migration InitialCreate
Update-Database
```

### Step 4: Configure Email Notifications (Optional)
1. Open `appsettings.json`
2. Add your SMTP settings:
```json
"EmailSettings": {
  "SmtpServer": "smtp.gmail.com",
  "SmtpPort": 587,
  "SenderEmail": "your-email@gmail.com",
  "SenderName": "Amaals Kitchen",
  "Password": "your-app-password",
  "EnableSsl": true,
  "EmailEnabled": true
}
```

### Step 5: Run the Application
1. Open the solution in Visual Studio
2. Press F5 or click "Run"
3. The application will launch in your default browser

---

## Default Admin Credentials

**Email:** admin@amaalskitchen.com  
**Password:** Admin123

**Important:** Change these credentials after first login in a production environment.

---

## Project Structure
```
AmaalsKitchen/
├── Controllers/           # MVC Controllers
│   ├── AccountController.cs
│   ├── AdminsController.cs
│   ├── HomeController.cs
│   ├── OrdersController.cs
│   └── CheckoutController.cs
├── Models/               # Data Models
│   ├── User.cs
│   ├── Order.cs
│   ├── Product.cs
│   ├── Cart.cs
│   └── ViewModels/
├── Views/                # Razor Views
│   ├── Account/
│   ├── Admins/
│   ├── Home/
│   └── Orders/
├── Data/                 # Database Context
│   └── ApplicationDbContext.cs
├── Services/             # Business Logic Services
│   ├── OrderService.cs
│   └── EmailService.cs
├── wwwroot/              # Static Files
│   ├── css/
│   ├── js/
│   └── images/
├── Migrations/           # EF Core Migrations
└── appsettings.json      # Configuration
```

---

## Usage Guide

### For Customers

1. **Register/Login**
   - Navigate to Account → Register
   - Fill in your details and create an account
   - Login with your credentials

2. **Browse Menu**
   - Click on "Menu" in the navigation bar
   - Use category filters to find items
   - View product details and prices

3. **Place Order**
   - Click "+ Add" on items you want
   - View cart by clicking the cart icon
   - Adjust quantities or remove items
   - Add special instructions (optional)
   - Click "Place Order"

4. **Track Order**
   - Navigate to "Orders" to view your order history
   - See real-time status updates
   - Check estimated pickup time

### For Admins

1. **Login**
   - Use admin credentials to login
   - You'll be redirected to the dashboard

2. **Manage Products**
   - Navigate to "+ AddProducts"
   - Fill in product details (name, price, category, image, description)
   - Click "Save Product"
   - Delete products using the delete button in the table

3. **Manage Orders**
   - Navigate to "All Orders"
   - View all customer orders
   - Filter by date (Today, Last 10 Days, Specific Date)
   - Update order status:
     - Click "Start Preparing" for pending orders
     - Click "Mark as Ready" when order is complete
     - Click "Mark as Completed" when customer collects
   - Cancel orders if needed

4. **View Analytics**
   - Dashboard shows key metrics automatically
   - View revenue, top products, and customer insights

---

## Database Schema

### Main Tables
- **Users** - Customer and admin accounts
- **Products** - Menu items
- **Orders** - Customer orders
- **OrderItems** - Individual items within orders

### Key Relationships
- Users → Orders (One-to-Many)
- Orders → OrderItems (One-to-Many)
- Products referenced in OrderItems (snapshot data)

---

## Security Features

- Password hashing using ASP.NET Core Identity PasswordHasher
- Session-based authentication with 30-minute timeout
- Anti-forgery tokens on all forms (CSRF protection)
- SQL injection prevention via Entity Framework parameterized queries
- Input validation on all forms
- HTTPS enforcement (production)

---

## Testing

### Manual Testing Completed
- User registration and login flows
- Product browsing and filtering
- Cart operations (add, update, remove)
- Order placement and tracking
- Admin product management
- Admin order management
- Email notification delivery
- Responsive design on multiple devices

### Test Credentials
**Customer Test Account:**
- Email: test@customer.com
- Password: Test123

**Admin Account:**
- Email: admin@amaalskitchen.com
- Password: Admin123

---

## Known Limitations

1. **Collection Only** - No delivery functionality (by design)
2. **Single Admin Level** - No role hierarchy (Owner vs Staff)
3. **No Real-Time Updates** - Uses 15-second auto-refresh instead of WebSockets
4. **Manual Inventory** - No automatic stock tracking
5. **Email Only** - WhatsApp/SMS notifications not implemented (planned for Phase 2)

---

## Future Enhancements (Phase 2)

- WhatsApp/SMS notifications integration
- Customer reviews and ratings
- Loyalty points system
- Delivery functionality
- Advanced reporting and analytics
- Mobile app (iOS/Android)
- Payment gateway integration
- Real-time updates using SignalR
- Inventory management system
- Multi-language support

---

## Documentation

Complete documentation is available in the `/Documentation` folder:

- **Systems Manual** - IT Administrator guide for installation and maintenance
- **User Manual** - Customer guide for using the system
- **Admin Manual** - Admin guide for managing the system
- **API Documentation** - Developer reference (if applicable)

---

## Troubleshooting

### Common Issues

**Issue:** Application won't start
**Solution:** 
- Verify .NET 8.0 SDK is installed (`dotnet --version`)
- Check SQL Server is running
- Verify connection string in appsettings.json

**Issue:** Database connection errors
**Solution:**
- Ensure SQL Server is running
- Check Windows Authentication is enabled
- Verify database name matches connection string
- Run migrations: `Update-Database`

**Issue:** Email notifications not sending
**Solution:**
- Verify SMTP settings in appsettings.json
- For Gmail, use App Password (not regular password)
- Check EmailEnabled is set to true

**Issue:** Images not displaying
**Solution:**
- Ensure images are in `/wwwroot/images/` folder
- Check image URLs in database
- Verify file permissions

For more troubleshooting help, see the Systems Manual.

---

## Contributing

This project was developed as an academic project for Varsity College. While it's not actively maintained for external contributions, feedback and suggestions are welcome.

---

## License

This project is developed for educational purposes as part of the INSY7315 module at Varsity College.

---

## Acknowledgments

- **Client:** Natheer (Amaals Kitchen) for providing the opportunity and requirements
- **Varsity College** for project guidance and resources
- **SimplyTech Solutions Team** for collaborative development
- **Bootstrap** for the responsive UI framework
- **Microsoft** for .NET and Entity Framework Core

---

## Contact

**Project Leader:** Ilyaas Kamish  
**Email:*ST10391174@vcconnect.edu.za*  
**GitHub:** [[Repository Link] ](https://github.com/INSY7135-Amaals-kitchen/AmaalsKitchen-Ganeef-AccountSection) 
**Project Website:** www.amaalskitchen.com (pending deployment)

---

## Project Status

✅ **Development Complete**  
✅ **Testing Complete**  
✅ **Documentation Complete**  
🎯 **Presentation:** November 4, 2025  
⏳ **Production Deployment:** Pending client approval

---

**Last Updated:** October 30, 2025  
**Version:** 1.0.0  
**Build Status:** Stable
T r i g g e r i n g   C I   r u n   1 1 / 0 6 / 2 0 2 5   1 6 : 3 4 : 4 3  
 