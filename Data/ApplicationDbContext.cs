using Microsoft.EntityFrameworkCore;
using AmaalsKitchen.Models;

namespace AmaalsKitchen.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<User> Users { get; set; }
    }
}
