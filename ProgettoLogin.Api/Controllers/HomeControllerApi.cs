using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;

namespace ProgettoLogin.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HomeControllerApi : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly string _pepper = Environment.GetEnvironmentVariable("pepperString");  //Variabile d'ambiente per le password
    
    
        public HomeControllerApi(ApplicationDbContext db)
        {
            _db = db;
        }

    
        //LOGIN                             LOGIN                           LOGIN
        //POST action method
        [HttpPost("Login")]
        public IActionResult LoginActionPost(String email, String password, bool openViewAdmin, int numberOfSitesToShow, int numberOfDaysToShow)
        {
            Account account = new Account();
            MainModel toPass = new MainModel();
            account.Email = email;
            bool b_Login = false;
        
            foreach (var accountNow in _db.Accounts)
            {
                if (accountNow.Email == account.Email)
                {
                    string passwordHash = ComputeHash(password, accountNow.PasswordSalt.ToString(), _pepper, 5000);
                    byte[] passwordHashByte = Encoding.UTF8.GetBytes(passwordHash);
                    if (passwordHashByte.SequenceEqual(accountNow.PasswordHash))        //Per fare il controllo dei valori tra due array
                    {
                        account = accountNow;
                        b_Login = true;
                    }
                }
            }
            
            if ((b_Login && account.id == 33) || openViewAdmin == true)
            {
                openViewAdmin = false;
                return Ok(CreateAdminModel(_db, numberOfSitesToShow, numberOfDaysToShow));   //Per visualizzare la pagina Admin
            }
            
            if (b_Login)
            {
                try
                {
                    return Ok(CreateMainModel(_db, account.id));
                }
                catch (NullReferenceException)
                {
                    //TempData["error"] = "Error with your Account";
                }
                finally
                {
                    b_Login = false;
                }
            }
            return NoContent();
        }
    
    

    
    
        //METODI NON HTTP       METODI NON HTTP     METODI NON HTTP
    
    
        //PAGINA MAIN
        //METODO PER CREARE IL MODEL MAIN PER VISUALIZZARE LA PAGINA MAIN
        public static MainModel CreateMainModel(ApplicationDbContext _db, int idAccountNow)
        {
            MainModel toPass = new MainModel();
        
            toPass.idAccount = idAccountNow;
            toPass.Email = _db.Accounts.Single(c => c.id == toPass.idAccount).Email!;
        
            foreach (var siteNow in _db.AccountXSites)
            {
                if (siteNow.idAccount == idAccountNow)
                {
                    try
                    {
                        toPass.DateRecording!.Add(siteNow.DateRecording);
                        toPass.Name!.Add(_db.Sites.Single(c => c.id == siteNow.idSite).Url!);
                    }
                    catch
                    {
                        //TempData["error"] = "One or more sites were not saved correctly";
                    }
                }
            }
            return toPass;
        }
        
        //PAGINA ADMIN
        //METODO PER CREARE IL MODEL ADMIN PER VISUALIZZARE LA PAGINA ADMIN
        public static AdminModel CreateAdminModel(ApplicationDbContext _db, int numberOfSitesToShow, int numberOfDaysToShow)
        {
            AdminModel adminModel = new AdminModel();
        
            if (numberOfSitesToShow == 0) numberOfSitesToShow = 5;      //Numero di default
            adminModel.allSitesCount = _db.AccountXSites.Where(x => x.idSite != null).Count();
            if (numberOfSitesToShow == 1) numberOfSitesToShow = _db.Sites.Count();
            adminModel.numberTopSavedSites = numberOfSitesToShow;
            adminModel.topSavedSites = TopSavedSites(_db, numberOfSitesToShow, adminModel.allSitesCount);
        
            if (numberOfDaysToShow == 0) numberOfDaysToShow = 14;      //Numero di default
            adminModel.numberOfDaysToShow = numberOfDaysToShow;
            adminModel.chartData = DrawTimeXSitesGraphic(_db, numberOfDaysToShow);
        
            return adminModel;
        }
        
        //PAGINA ADMIN
        //CREA UNA LISTA DI SITI E LI CLASSIFICA IN BASE ALLA PERCENTUALE DI SALVATAGGI
        public static List<(string Url, int SavePerc)> TopSavedSites(ApplicationDbContext _db, int numberOfSites, int allSitesCount)
        {
            List<String?> allSites = new();
        
            foreach (var siteNow in _db.AccountXSites)
            {
                allSites.Add(_db.Sites.Single(c => c.id == siteNow.idSite).Url);
            }
        
        
            var savedSites = allSites.GroupBy(s => s)       //Raggruppa tutti gli elementi con lo stesso url
                                .Select(group => new { Url = group.Key, Count = group.Count() })    //Raggruppa i siti per URL e conta il numero di volte che ogni URL appare
                                .OrderByDescending(s => s.Count)        //Ordina la lista di strutture anonime in base alla proprietà Count
                                .Take(numberOfSites);
        
            List<(string Url, int SavePerc)> topSavedSites = new List<(string Url, int SavePerc)>();
        
            foreach (var savedSite in savedSites)
            {
                int savePerc = (savedSite.Count * 100) / allSitesCount;
                topSavedSites.Add((savedSite.Url, savePerc));
            }
            return topSavedSites!;
        }
        
        //PAGINA ADMIN
        //METODO PER DISEGNARE IL GRAFICO PER REGISTRARE QUANDO GLI UTENTI REGISTRANO UN NUOVO ACCOUNT
        //X:TEMPO  -  Y:NUMERO UTENTI 
        //UTILIZZO LA LIBRERIA Chart.js
        public static List<(DateTime DateRecording, int NumberOfAccount)> DrawTimeXSitesGraphic(ApplicationDbContext _db, int numberOfDays)
        {
            List<(DateTime DateRecording, int NumberOfAccount)> chartData = new List<(DateTime, int)>();
        
            DateTime lastDate = DateTime.Now;
            DateTime firstDate;
            if (numberOfDays == 1)                                      //dal primo giorno
            {
                firstDate = _db.AccountXSites
                           .OrderBy(c => c.DateRecording)
                           .Select(c => c.DateRecording)
                           .FirstOrDefault();
        
                numberOfDays = lastDate.Subtract(firstDate).Days;       //Calcola i giorni tra firstDate e lastDate
            }
            else
            {
                firstDate = lastDate.AddDays(-numberOfDays);            //Numero di giorni passato da html
            }
        
            if (numberOfDays % 14 != 0) numberOfDays = numberOfDays + (14 - (numberOfDays % 14));   //Controlla se è un multiplo di 14, altrimenti aggiunge i giorni mancanti per la corretta visualizzazione del grafico
        
            TimeSpan interval = TimeSpan.FromDays(numberOfDays / 14);       //Crea una nuova istanza di TimeSpan con la durata specificata in giorni
            DateTime current = firstDate;
        
            while (current <= lastDate)     //Dividere il range di date in 14 parti uguali, inizializza il numero di account per ogni data con 0
            {
                chartData.Add((current, 0));
                current = current.Add(interval);
            }
            chartData.Add((DateTime.Now, 0));
        
            foreach (var siteNow in _db.AccountXSites)      //Incrementare il numero di account per ogni data in cui è presente un accoun
            {
                if (siteNow.DateRecording >= firstDate && siteNow.DateRecording <= lastDate)
                {
                    int index = (int)((siteNow.DateRecording - firstDate).TotalDays / interval.TotalDays);
                    chartData[index] = (chartData[index].DateRecording, chartData[index].NumberOfAccount + 1);
                }
            }
        
            for (int index1 = 0; index1 < 15; index1++)        //Controlla che ci siano tutti i 15 oggetti della lista. Da 0 a 14.
            {
                if (index1 >= chartData.Count)
                {
                    chartData.Add((chartData[index1 - 1].DateRecording, chartData[index1 - 1].NumberOfAccount));
        
                    for (int index2 = index1; index2 > 0; index2--)     //Sposta tutti gli elementi della lista di un indice per crearne uno nuovo all'inizio
                    {
                        chartData[index2] = chartData[index2 - 1];
                    }
        
                    chartData[0] = (chartData[1].DateRecording.Subtract(interval), 0);  //Riscrive chartData[0]. Per visualizzare la parte sinistra del grafico. DateRecording sottrae l'intervallo.
                }
            }
            return chartData;
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
}