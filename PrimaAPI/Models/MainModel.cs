using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace ProgettoLogin.Models;

[Keyless]
public class MainModel
{
    public int idAccount { get; set; }
    public string? Email { get; set; }
    public List<string?> Name { get; set; } = new();
    public List<DateTime?> DateRecording { get; set; } = new();

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
    public string? city { get; set; }
    public string? country { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Timezone { get; set; }
    public DateTime? Localtime { get; set; }


}