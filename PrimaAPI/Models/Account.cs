using System.ComponentModel.DataAnnotations;

namespace CarrelloLogin.Models;
public class Account
{
    [Key]
    public int id { get; set; }

    //EMAIL
    [Required(ErrorMessage = "Email is required")]
    [StringLength(16, ErrorMessage = "Must be between 5 and 16 characters", MinimumLength = 5)]
    [RegularExpression("^[a-zA-Z0-9_.-]+@[a-zA-Z0-9-]+.[a-zA-Z0-9-.]+$", ErrorMessage = "Must be a valid email")]
    public string? Email { get; set; }

    public byte[]? PasswordHash { get; set; }
    public byte[]? PasswordSalt { get; set; }
    public byte[]? modelFile { get; set; }

    public string? Ip { get; set; }

    public int? NumberPhoto { get; set; }

}