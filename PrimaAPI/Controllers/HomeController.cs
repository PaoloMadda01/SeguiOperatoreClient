using Microsoft.AspNetCore.Mvc;
using ProgettoLogin.Data;
using ProgettoLogin.Models;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Octokit;
using System.Globalization;
using System.Xml;

namespace ProgettoLogin.Controllers;

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
        client.BaseAddress = new Uri("http://192.168.181.129:8000/");   //Indirizzo IP della tua macchina virtuale
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
    public async Task<IActionResult> LoginActionPostAsync(Login login, bool openViewAdmin, int numberOfSitesToShow, int numberOfDaysToShow)
    {
        Account account = new Account();
        MainModel toPass = new MainModel();
        account.Email = login.Email;
        bool b_Login = false;
        String timeZoneApi;

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
                }
            }
        }

        if((b_Login && account.id == 33) || openViewAdmin == true)
        {
            openViewAdmin = false;
            return View("Admin", CreateAdminModel(numberOfSitesToShow, numberOfDaysToShow));   //Per visualizzare la pagina Admin
        }
        
        if (b_Login)
        {
            MainModel model = await CreateMainModelAsync(account.id);
            return View("Main", model);
                
        }
        return View("Login");
    }



    //CAMERA LOGIN              CAMERA LOGIN                  CAMERA LOGIN              CAMERA LOGIN
    [HttpPost]
    public async Task<IActionResult> CameraLogin(IFormFile photo, string email)
    {
        if (photo == null || !photo.ContentType.StartsWith("image/"))
        {
            return BadRequest("Errore con la foto");
        }

        bool b_log = false;
        byte[] photoNow;
        XmlDocument xmlFacialRecognition;

        // Verifica che il file sia un'immagine
        if (photo.ContentType.StartsWith("image/"))
        {
            // Leggi il contenuto del file in un array di byte
            using (var memoryStream = new MemoryStream())
            {
                photo.CopyTo(memoryStream);
                photoNow = memoryStream.ToArray();
            }

            foreach (var accountNow in _db.Accounts)
            {
                if (accountNow.Email == email)
                {
                    //xmlFacialRecognition = _db.Accounts.Single(c => c.id == accountNow.id).modelFile;
                    xmlFacialRecognition = null;
                    if (xmlFacialRecognition == null)
                    {
                        TempData["error"] = "You haven't a photo in database";
                        break;
                    }

                    bool b_Login = await ProcessImageApi(xmlFacialRecognition, photoNow);
                    if (b_Login)
                    {
                        MainModel model = await CreateMainModelAsync(accountNow.id);
                        return View("Main", model);
                    }
                }
            }
        }
        return View("Login");
    }



    //PAGINA MAIN
    //METODO PER CREARE IL MODEL MAIN PER VISUALIZZARE LA PAGINA MAIN
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<MainModel> CreateMainModelAsync(int idAccountNow)     //async Perchè deve inviare e ricevere i dati dal server dell'Api
    {
        MainModel toPass = new MainModel();

        toPass.idAccount = idAccountNow;
        toPass.Email = _db.Accounts.Single(c => c.id == toPass.idAccount).Email!;
        String cityName = _db.Accounts.Single(c => c.id == toPass.idAccount).CityName!;

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
                    TempData["error"] = "One or more sites were not saved correctly";
                }
            }
        }

        dynamic data = JsonConvert.DeserializeObject(await TimeZoneApiMethodAsync(cityName));

        toPass.city = data.location.name;
        toPass.country = data.location.country;
        toPass.Latitude = data.location.lat;
        toPass.Longitude = data.location.lon;
        toPass.Timezone = data.location.tz_id;
        toPass.Localtime = DateTime.Parse((string)data["location"]["localtime"]);

        return toPass;
    }

    public async Task<string> TimeZoneApiMethodAsync(String cityName)
    {
        String timeZone;
        var client = new HttpClient();
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri($"https://weatherapi-com.p.rapidapi.com/timezone.json?q={cityName}"),
            Headers =
                    {
                        { "X-RapidAPI-Key", "dd280ef06cmsh3bae67861601777p105672jsn28f2aed5b3cc" },
                        { "X-RapidAPI-Host", "weatherapi-com.p.rapidapi.com" },
                    },
        };
        using (var response = await client.SendAsync(request))
        {
            response.EnsureSuccessStatusCode();
            timeZone = await response.Content.ReadAsStringAsync();
        }
        return timeZone;
    }

    //PAGINA ADMIN
    //METODO PER CREARE IL MODEL ADMIN PER VISUALIZZARE LA PAGINA ADMIN
    public AdminModel CreateAdminModel(int numberOfSitesToShow, int numberOfDaysToShow)
    {
        AdminModel adminModel = new AdminModel();

        if (numberOfSitesToShow == 0) numberOfSitesToShow = 5;      //Numero di default
        adminModel.allSitesCount = _db.AccountXSites.Where(x => x.idSite != null).Count();
        if (numberOfSitesToShow == 1) numberOfSitesToShow = _db.Sites.Count();
        adminModel.numberTopSavedSites = numberOfSitesToShow;
        adminModel.topSavedSites = TopSavedSites(numberOfSitesToShow, adminModel.allSitesCount);

        if (numberOfDaysToShow == 0) numberOfDaysToShow = 14;      //Numero di default
        adminModel.numberOfDaysToShow = numberOfDaysToShow;
        adminModel.chartData = DrawTimeXSitesGraphic(numberOfDaysToShow);

        return adminModel;
    }

    //PAGINA ADMIN
    //CREA UNA LISTA DI SITI E LI CLASSIFICA IN BASE ALLA PERCENTUALE DI SALVATAGGI
    [HttpPost]
    [ValidateAntiForgeryToken]
    public List<(string Url, int SavePerc)> TopSavedSites(int numberOfSites, int allSitesCount)
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
    [HttpPost]
    [ValidateAntiForgeryToken]
    public List<(DateTime DateRecording, int NumberOfAccount)> DrawTimeXSitesGraphic(int numberOfDays)
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
            if(index1 >= chartData.Count)
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



    

    public async Task<bool> ProcessImageApi(XmlDocument xmlFacialRecognition, byte[] photoNow)
    {
        var content = new MultipartFormDataContent();
        content.Add(new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(xmlFacialRecognition.OuterXml))), "xmlFacialRecognition");
        content.Add(new ByteArrayContent(photoNow), "photoNow", "photoNow.jpg");

        HttpResponseMessage response = await client.PostAsync("process_image/", content);
        response.EnsureSuccessStatusCode();
        string responseBody = await response.Content.ReadAsStringAsync();
        double score = double.Parse(responseBody, CultureInfo.InvariantCulture);
        Console.WriteLine(score);
        if (score >= 0.7) return true;
        else return false;
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