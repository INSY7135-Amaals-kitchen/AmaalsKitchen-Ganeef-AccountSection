using System.ComponentModel.DataAnnotations;

namespace AmaalsKitchen.Models
{
    public class EditProfileViewModel
    {
     
        [EmailAddress]
        public string Email { get; set; } = "";

        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = "";

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = "";

        [Required]
        [Phone]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = "";
    }
}
