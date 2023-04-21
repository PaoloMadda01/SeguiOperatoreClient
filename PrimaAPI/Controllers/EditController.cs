using Microsoft.AspNetCore.Mvc;
using ProgettoLogin.Data;
using ProgettoLogin.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Site = ProgettoLogin.Models.Site;
using Newtonsoft.Json;
using Octokit;

namespace ProgettoLogin.Controllers;
public class EditController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly string _pepper = Environment.GetEnvironmentVariable("pepperString");  //Variabile d'ambiente per le password
    private readonly HttpClient client;


    public EditController(ApplicationDbContext db)
    {
        _db = db;

        client = new HttpClient();
        client.BaseAddress = new Uri("http://192.168.181.129:8000/");   //Indirizzo IP della macchina virtuale
    }

    //GET action method
    public IActionResult Create()
    {
        return View();
    }

    //POST action method
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(Login login, String cityName)
    {
        Account account = new Account();
        account.Email = login.Email;
        if (cityName != null)
        {
            account.CityName = cityName;
        }
        else
        {
            ModelState.AddModelError("City", "City is required");
            TempData["error"] = "Try another email";
            return Redirect("~/Edit/Create");
        }
        if (account.Email == login.Pass) ModelState.AddModelError("Email", "Error with your password");

        foreach (var accountNow in _db.Accounts)
        {
            if (account.Email == accountNow.Email)
            {
                ModelState.AddModelError("Email", "This Email already exists");
                TempData["error"] = "Try another email";
                return Redirect("~/Edit/Create");
            }
        }

        string salt = GenerateSalt();
        account.PasswordSalt = Encoding.UTF8.GetBytes(salt);
        string passwordHash = ComputeHash(login.Pass, account.PasswordSalt.ToString(), _pepper, 5000);    //Stringa che si vuole crittografare,
                                                                                //5000: iteration è il numero di volte in cui il metodo ComputeHash viene eseguito.
        account.PasswordHash = Encoding.UTF8.GetBytes(passwordHash);        //Conversione da string a byte[] per aver maggior sicurezza
        
        _db.Accounts.Add(account);
        _db.SaveChanges();

        TempData["success"] = "Account created successfully";
        return View("AddSite", account);
    }



    [HttpPost]
    public async Task<IActionResult> AddPhotoAsync(IFormFile photo, string email)
    {
        bool b_tempData = false;
        int idAccountNow = 0;

        foreach (var accountNow in _db.Accounts)
        {
            if (accountNow.Email == email)
            {
                using (var memoryStream = new MemoryStream())           //Crea un nuovo oggetto MemoryStream per memorizzare temporaneamente i dati dell'immagine dal FormData
                {
                    photo.CopyTo(memoryStream);

                    accountNow.modelFile = await UpdateModelFile(_db.Accounts.Single(c => c.id == accountNow.id).modelFile, memoryStream.ToArray());

                    _db.Accounts.Single(c => c.id == accountNow.id).modelFile = accountNow.modelFile;     //Converte i dati dell'immagine in un array di byte e li salva ne db
                }
                b_tempData = true;
                idAccountNow = accountNow.id;
                break;
            }
        }    
    
        _db.SaveChanges();
        TempData["success"] = "Success";

        MainModel model = await CreateMainModelAsync(idAccountNow);
        return View("~/Views/Home/Main.cshtml", model);
    }



    //CHANGE PASSWORD       CHANGE PASSWORD         CHANGE PASSWORD         CHANGE PASSWORD
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassAsync(MainModel mainModel)
    {
        bool b_tempData = false;
        foreach (var accountNow in _db.Accounts)
        {
            if(mainModel.idAccount == accountNow.id && mainModel.PassNew == mainModel.PassNewR)
            {
                string passwordHash = ComputeHash(mainModel.PassNow, accountNow.PasswordSalt.ToString(), _pepper, 5000);
                byte[] passwordHashByte = Encoding.UTF8.GetBytes(passwordHash);
                if (passwordHashByte.SequenceEqual(accountNow.PasswordHash))        //Per fare il controllo dei valori tra due array
                {
                    string saltNew = GenerateSalt();
                    accountNow.PasswordSalt = Encoding.UTF8.GetBytes(saltNew);
                    string passwordHashNew = ComputeHash(mainModel.PassNew, accountNow.PasswordSalt.ToString(), _pepper, 5000);    //Stringa che si vuole crittografare,
                                                                                                                                  //5000: iteration è il numero di volte in cui il metodo ComputeHash viene eseguito.
                    accountNow.PasswordHash = Encoding.UTF8.GetBytes(passwordHashNew);        //Conversione da string a byte[] per aver maggior sicurezza

                    _db.Accounts.Single(c => c.id == accountNow.id).PasswordSalt = accountNow.PasswordSalt;
                    _db.Accounts.Single(c => c.id == accountNow.id).PasswordHash = accountNow.PasswordHash;
                    b_tempData = true;
                }
            }
        }

        if (b_tempData)
        {
            _db.SaveChanges(); 
            TempData["success"] = "Success";
        }
        else
        {
            TempData["error"] = "Error";
        }

        MainModel model = await CreateMainModelAsync(mainModel.idAccount);
        return View("~/Views/Home/Main.cshtml", model);
    }




    //ADDSITE            ADDSITE                  ADDSITE                  
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddSite(int? idAccount)
    {
        var accountFromDb = _db.Accounts.Find(idAccount);

        if (accountFromDb == null) return NotFound();

        return View(accountFromDb);
    }

    //POST action method
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddSitePostAsync(String siteNowStr, Account account)
    {
        int indexNewSite;
        var accountNow = _db.Accounts.Find(account.id);

        Regex regex = new Regex("^www\\.[a-zA-Z0-9]+\\.[a-zA-Z]");      //regular expression

        if (regex.IsMatch(siteNowStr!))
        {
            // URL è valido
            try
            {
                indexNewSite = _db.Sites.Single(c => c.Url == siteNowStr).id;
            }
            catch
            {
                Site siteNowObj = new Site();
                siteNowObj.Url = siteNowStr;
                _db.Sites.Add(siteNowObj);
                try
                {
                    _db.SaveChanges();
                }
                catch
                {
                    TempData["error"] = "The site is incorrect, you can only add www sites";
                    return View("AddSite", accountNow);
                }
                indexNewSite = _db.Sites.Single(c => c.Url == siteNowStr).id;
            }

            bool b_AddNewSite = true;
            foreach (var nameSite in _db.AccountXSites)
            {
                if (nameSite.idSite == indexNewSite && nameSite.idAccount == account.id)
                {
                    b_AddNewSite = false;
                    TempData["success"] = "This site is already saved in your account";
                    break;
                }
            }
            if (b_AddNewSite)
            {
                AccountXSite addNewSite = new AccountXSite();
                addNewSite.idAccount = account.id;
                addNewSite.idSite = indexNewSite;
                addNewSite.DateRecording = DateTime.Now;

                _db.AccountXSites.Add(addNewSite);
                _db.SaveChanges();
                TempData["success"] = "Site updated successfully";
            }
            try
            {
                TempData["success"] = "Success";
                MainModel model = await CreateMainModelAsync(account.id);
                return View("~/Views/Home/Main.cshtml", model);
            }
            catch (NullReferenceException)
            {
                TempData["error"] = "Error with your Account";
            }
        }
        else
        {
            // URL non è valido
            TempData["error"] = "The site is incorrect";
            return View("AddSite", accountNow);
        }

        return Redirect("~/Home/Login");
    }


    //DELETE                DELETE                  DELETE
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteAccount(int? idAccount)
    {
        var accountToRemove = _db.Accounts.Find(idAccount);
        _db.Accounts.Remove(accountToRemove!);

        foreach (var accountXSiteToRemove in _db.AccountXSites)
        {
            if (accountXSiteToRemove.idAccount == idAccount) _db.AccountXSites.Remove(accountXSiteToRemove!);
        }

        _db.SaveChanges();
        TempData["success"] = "Account deleted successfully";
        return Redirect("~/Home/Login");
    }



    //DELETE SITE            DELETE SITE             DELETE SITE
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteSite(String? siteToDeleteString, int? idAccountNow)
    {
        var SiteObj = _db.Sites.Find(_db.Sites.Single(c => c.Url == siteToDeleteString).id!);
        var AccountXSitesObj = _db.AccountXSites.Find(_db.AccountXSites.Single(c => c.idSite == SiteObj!.id && c.idAccount == idAccountNow).id!);

        if (AccountXSitesObj == null) return NotFound();

        if (AccountXSitesObj.idAccount == idAccountNow && AccountXSitesObj.idSite == SiteObj!.id)
        {
            _db.AccountXSites.Remove(AccountXSitesObj!);
            _db.SaveChanges();
            TempData["success"] = "Site deleted successfully";
        }
        try
        {
            TempData["success"] = "Success";
            return View("~/Views/Home/Main.cshtml", CreateMainModelAsync(AccountXSitesObj!.idAccount));
        }
        catch (NullReferenceException)
        {
            TempData["error"] = "Error with your Account";
        }

        return Redirect("~/Home/Login");
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


    //METODO PER VISUALIZZARE LA PAGINA MAIN
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<MainModel> CreateMainModelAsync(int idAccountNow)
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



    //Invia il file pth del modello e la nuova foto per aggiornare il modello
    public async Task<byte[]> UpdateModelFile(byte[] modelFacialRecognition, byte[] photoNow)
    {
        var content = new MultipartFormDataContent();
        if (modelFacialRecognition != null)
        {
            content.Add(new ByteArrayContent(modelFacialRecognition), "model", "model.pth");
        }
        content.Add(new ByteArrayContent(photoNow), "photoNow", "photoNow.jpg");

        HttpResponseMessage response = await client.PostAsync("update_model/", content);
        response.EnsureSuccessStatusCode();
        byte[] responseData = await response.Content.ReadAsByteArrayAsync();

        return responseData;
    }

}