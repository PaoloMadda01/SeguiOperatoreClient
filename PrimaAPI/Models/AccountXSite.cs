using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace ProgettoLogin.Models;
    public class AccountXSite
    {
        [Key]
        public int id { get; set; } 
        public int idAccount { get; set; }
        public int idSite { get; set; }
        public DateTime DateRecording { get; set; }
    }
