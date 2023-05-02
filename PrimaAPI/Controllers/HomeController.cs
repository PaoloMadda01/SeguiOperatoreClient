using Microsoft.AspNetCore.Mvc;
using CarrelloLogin.Data;
using CarrelloLogin.Models;
using System.Security.Cryptography;
using System.Text;
using Account = CarrelloLogin.Models.Account;

namespace CarrelloLogin.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _db;
    private readonly string _pepper = Environment.GetEnvironmentVariable("pepperString");  //Variabile d'ambiente per le password
    private readonly HttpClient client;


    public HomeController(ILogger<HomeController> logger, ApplicationDbContext db)
    {
        _logger = logger;                               //Per caricare la prima pagina
        _db = db;                                       //Per il database

        client = new HttpClient();
    }

    public IActionResult Login()
    {
        IEnumerable<Account> objAccountList = _db.Accounts;
        return View();
    }



    //LOGIN                             LOGIN                           LOGIN

    //POST action method
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginActionPostAsync(Login login)
    {
        Account account = new Account();
        account.Email = login.Email;
        bool b_Login = false;

        foreach (var accountNow in _db.Accounts)
        {
            if (accountNow.Email == account.Email)
            {
                string passwordHash = ComputeHash(login.Pass, accountNow.PasswordSalt.ToString(), _pepper, 5000);
                byte[] passwordHashByte = Encoding.UTF8.GetBytes(passwordHash);
                if (passwordHashByte.SequenceEqual(accountNow.PasswordHash))        //Per fare il controllo dei valori tra due array
                {
                    account = accountNow;
                    b_Login = true;
                    break;
                }
            }
        }
        
        if (b_Login)
        {
            MainModel model = await CreateMainModelAsync(account.id);
            return View("~/Views/Home/Main.cshtml", model);

        }
        return View("Login");
    }




    //METODO PER VISUALIZZARE LA PAGINA MAIN
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<MainModel> CreateMainModelAsync(int idAccountNow)
    {
        MainModel toPass = new MainModel();

        toPass.idAccount = idAccountNow;
        toPass.Email = _db.Accounts.Single(c => c.id == toPass.idAccount).Email!;
        toPass.Ip = _db.Accounts.Single(c => c.id == toPass.idAccount).Ip;
        toPass.NumberPhoto = _db.Accounts.Single(c => c.id == toPass.idAccount).NumberPhoto;
        toPass.Connection = await ConnectionApi(toPass.Ip);

        return toPass;
    }


    public async Task<bool> ConnectionApi(string? ip)
    {
        try
        {
            client.BaseAddress = new Uri("http://" + ip + ":8000/");
            client.Timeout = TimeSpan.FromSeconds(4); // Imposta il timeout a 5 secondi
            HttpResponseMessage response = await client.GetAsync("connection_api/");
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (TaskCanceledException ex)
        {
            if (ex.InnerException is TimeoutException)
            {
                return false;
            }
            throw;
        }
    }





    //Metodi per salvare in modo corretto le password con Hash, Salt, Papper e iteration
    //SHA256
    public static string ComputeHash(string password, string salt, string pepper, int iteration)
    {
        if (iteration <= 0) return password;

        using var sha256 = SHA256.Create();
        var passwordSaltPepper = $"{password}{salt}{pepper}";
        byte[] byteValue = Encoding.UTF8.GetBytes(passwordSaltPepper);
        var byteHash = sha256.ComputeHash(byteValue);
        var hash = Convert.ToBase64String(byteHash);
        return ComputeHash(hash, salt, pepper, iteration - 1);
    }

    public static string GenerateSalt()
    {
        using var rng = RandomNumberGenerator.Create();
        var byteSalt = new byte[16];
        rng.GetBytes(byteSalt);
        var salt = Convert.ToBase64String(byteSalt);
        return salt;
    }
}