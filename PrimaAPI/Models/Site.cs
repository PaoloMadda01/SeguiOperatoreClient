using System.ComponentModel.DataAnnotations;

namespace ProgettoLogin.Models;
public class Site
{
    [Key]
    public int id { get; set; }

    public string? Url { get; set; }
}