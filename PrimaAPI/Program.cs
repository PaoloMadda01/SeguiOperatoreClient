using CarrelloLogin.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(        //Per utilizzare la dependency injection (oggetto da cui dipende un altro oggetto)
    builder.Configuration.GetConnectionString("DefaultConnection")          //Configurazione della stringa di connessione "DefaultConnection" per il database
    ));

builder.Services.AddRazorPages().AddRazorRuntimeCompilation();
//Per cambiare il carattere anche della navigation bar

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    //app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Login}/{id?}");                  //Attiva la prima pagina 

app.Run();
