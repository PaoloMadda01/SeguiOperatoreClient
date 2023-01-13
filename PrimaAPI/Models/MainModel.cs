using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace ProgettoLogin.Models;

[Keyless]
public class MainModel
{
    public int idAccount { get; set; }
    public string? Email { get; set; }
    public List<string?> Name { get; set; } = new();
    public List<DateTime?> DateRecording { get; set; } = new();

}
