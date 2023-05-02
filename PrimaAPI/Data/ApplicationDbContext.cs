using Microsoft.EntityFrameworkCore;
using CarrelloLogin.Models;

namespace CarrelloLogin.Data;
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    //Metodo per leggere dal database e inviare i dati alla classe
    {

    }

    public DbSet<Account> Accounts { get; set; }

}
