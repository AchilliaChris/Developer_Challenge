using System.ComponentModel.DataAnnotations;
namespace DeveloperChallenge.ViewModels
{
    public class CustomerViewModel
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        [Required]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        public string Email { get; set; } = string.Empty;
        [Required]
        [Phone]
        [RegularExpression(@"^\+?[0-9\s\-\(\)]{7,}$", ErrorMessage = "Invalid phone number format.")]
        public string Phone { get; set; } = string.Empty;
    }
}
