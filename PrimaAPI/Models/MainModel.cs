using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace CarrelloLogin.Models;

[Keyless]
public class MainModel
{
    public int idAccount { get; set; }
    public string? Email { get; set; }

    //PASSWORDS
    [DisplayName("Password")]
    [Required(ErrorMessage = "Password is required")]
    [StringLength(20, ErrorMessage = "Must be between 5 and 20 characters", MinimumLength = 5)]
    [DataType(DataType.Password)]
    public string? PassNow { get; set; }

    [DisplayName("Password2")]
    [Required(ErrorMessage = "Password is required")]
    [StringLength(20, ErrorMessage = "Must be between 5 and 20 characters", MinimumLength = 5)]
    [DataType(DataType.Password)]
    public string? PassNew { get; set; }

    [DisplayName("Password3")]
    [Required(ErrorMessage = "Password is required")]
    [StringLength(20, ErrorMessage = "Must be between 5 and 20 characters", MinimumLength = 5)]
    [DataType(DataType.Password)]
    public string? PassNewR { get; set; }

    public string? Ip { get; set; }

    public int? NumberPhoto { get; set; }

    public bool Connection { get; set; }

}