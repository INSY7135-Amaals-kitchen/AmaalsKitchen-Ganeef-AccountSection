using System.ComponentModel.DataAnnotations;

namespace AmaalsKitchen.Models
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        public string Email { get; set; } = "";
    }
}
