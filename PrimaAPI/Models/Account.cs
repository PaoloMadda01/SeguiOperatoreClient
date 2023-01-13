using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace ProgettoLogin.Models;
    public class Account
    {
        [Key]
        public int id { get; set; }

        //EMAIL
        [Required(ErrorMessage = "Email is required")]
        [StringLength(16, ErrorMessage = "Must be between 5 and 50 characters", MinimumLength = 5)]
        [RegularExpression("^[a-zA-Z0-9_.-]+@[a-zA-Z0-9-]+.[a-zA-Z0-9-.]+$", ErrorMessage = "Must be a valid email")]
        public string? Email { get; set; }

        //PASSWORD
        [DisplayName("Password")]
        [Required(ErrorMessage = "Password is required")]
        [StringLength(255, ErrorMessage = "Must be between 5 and 255 characters", MinimumLength = 5)]
        [DataType(DataType.Password)]
        public string? Pass { get; set; }
    }