using Microsoft.AspNetCore.Mvc;
using CarrelloLogin.Data;
using CarrelloLogin.Models;
using System.Security.Cryptography;
using System.Text;
using Account = CarrelloLogin.Models.Account;
using System.Globalization;
using Octokit;
using System.Security.Principal;
using Microsoft.CodeAnalysis.Differencing;

namespace CarrelloLogin.Controllers;
public class EditController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly string _pepper = Environment.GetEnvironmentVariable("pepperString");  //Variabile d'ambiente per le password
    private readonly HttpClient client;


    public EditController(ApplicationDbContext db)
    {
        _db = db;

        client = new HttpClient();
        client.BaseAddress = new Uri("http://" + 0 + ":8000/");
    }

    //GET action method
    public IActionResult Create()
    {
        return View();
    }



    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Follow(int idAccount)
    {
        var account = _db.Accounts.Single(c => c.id == idAccount);
        var modelMain = await CreateMainModelAsync(idAccount);

        if (account.Ip == null)
        {
            TempData["error"] = "IP address not set for this account";
            return View("~/Views/Edit/Main.cshtml", modelMain);
        }

        using var client = new HttpClient();
        client.BaseAddress = new Uri("http://" + account.Ip + ":8000/");
        //client.Timeout = TimeSpan.FromSeconds(4); // Imposta il timeout a 5 secondi

        var content = new MultipartFormDataContent();
        var modelRecognition = account.modelFile;

        if (modelRecognition == null)
        {
            TempData["error"] = "Model file not set for this account, add at least one photo";
            return View("~/Views/Edit/Main.cshtml", modelMain);
        }

        if (modelMain.Connection)
        {
            content.Add(new ByteArrayContent(modelRecognition), "model", "model.pth");
            client.BaseAddress = new Uri("http://" + account.Ip + ":8000/");
            client.Timeout = TimeSpan.FromSeconds(4); // Imposta il timeout a 4 secondi

            try
            {
                HttpResponseMessage response = await client.GetAsync("connection_api/");

                // Invia la richiesta POST al server
                var postTask = client.PostAsync("process_image/", content);

                return View("~/Views/Edit/Following.cshtml", modelMain);
            }

            catch (HttpRequestException)
            {
                Stop(modelMain.idAccount);
                TempData["error"] = "Error with API";
                return View("~/Views/Home/Main.cshtml", modelMain);
            }
            catch (TaskCanceledException ex)
            {
                if (ex.InnerException is TimeoutException)
                {
                    Stop(modelMain.idAccount);
                    TempData["error"] = "You are not connect";
                    return View("~/Views/Home/Main.cshtml", modelMain);
                }
                throw;
            }
            
        }

        Stop(modelMain.idAccount);
        TempData["error"] = "Disconnect";
        return View("~/Views/Home/login.cshtml");
    }



    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Stop(int idAccount)
    {
        var account = _db.Accounts.Single(c => c.id == idAccount);
        var modelMain = await CreateMainModelAsync(idAccount);

        if (account.Ip == null)
        {
            TempData["error"] = "IP address not set for this account";
            return View("~/Views/Edit/Main.cshtml", modelMain);
        }

        using var client = new HttpClient();
        client.BaseAddress = new Uri("http://" + account.Ip + ":8000/");

        var content = new MultipartFormDataContent();
        var modelRecognition = account.modelFile;

        if (modelRecognition == null)
        {
            TempData["error"] = "Model file not set for this account, add at least one photo";
            return View("~/Views/Edit/Main.cshtml", modelMain);
        }

        try
        {
            client.BaseAddress = new Uri("http://" + modelMain.Ip + ":8000/");
            client.Timeout = TimeSpan.FromSeconds(4); // Imposta il timeout a 5 secondi
            HttpResponseMessage response = await client.GetAsync("stop_processing/");
            if (response.IsSuccessStatusCode)
            {
                TempData["success"] = "Stop following";
                return View("~/Views/Edit/Main.cshtml", modelMain);
            }
            else
            {
                TempData["error"] = "Error with server";
            }
        }
        catch (HttpRequestException)
        {
            TempData["error"] = "Error with server";
        }
        catch (TaskCanceledException ex)
        {
            client.BaseAddress = new Uri("http://" + modelMain.Ip + ":8000/");
            client.Timeout = TimeSpan.FromSeconds(4); // Imposta il timeout a 5 secondi
            HttpResponseMessage response = await client.GetAsync("stop_processing/");
            if (response.IsSuccessStatusCode)
            {
                TempData["success"] = "Stop following";
                return View("~/Views/Edit/Main.cshtml", modelMain);
            }
            else
            {
                TempData["error"] = "Error with server, timeout";
            }
            throw;
        }

        return View("~/Views/Edit/Following.cshtml", modelMain);

    }





    //POST action method
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Create create)
    {
        Account account = new Account();
        account.Email = create.Email;

        if (account.Email == create.Pass) ModelState.AddModelError("Email", "Error with your password");

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
        string passwordHash = ComputeHash(create.Pass, account.PasswordSalt.ToString(), _pepper, 5000);    //Stringa che si vuole crittografare,
                                                                                //5000: iteration è il numero di volte in cui il metodo ComputeHash viene eseguito.
        account.PasswordHash = Encoding.UTF8.GetBytes(passwordHash);        //Conversione da string a byte[] per aver maggior sicurezza
        account.NumberPhoto = 0;

        _db.Accounts.Add(account);
        _db.SaveChanges();

        MainModel model = await CreateMainModelAsync(account.id);
        return View("~/Views/Home/Main.cshtml", model);
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
                client.BaseAddress = new Uri("http://" + accountNow.Ip + ":8000/");   //Indirizzo IP della macchina virtuale

                using (var memoryStream = new MemoryStream())           //Crea un nuovo oggetto MemoryStream per memorizzare temporaneamente i dati dell'immagine dal FormData
                {
                    photo.CopyTo(memoryStream);

                    accountNow.modelFile = await UpdateModelFile(_db.Accounts.Single(c => c.id == accountNow.id).modelFile, memoryStream.ToArray());

                    _db.Accounts.Single(c => c.id == accountNow.id).modelFile = accountNow.modelFile;     //Converte i dati dell'immagine in un array di byte e li salva ne db
                    _db.Accounts.Single(c => c.id == accountNow.id).NumberPhoto = accountNow.NumberPhoto + 1;
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




    //CHANGE IP       CHANGE IP         CHANGE IP         CHANGE IP
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeIpAsync(MainModel mainModel)
    {
        _db.Accounts.Single(c => c.id == mainModel.idAccount).Ip = mainModel.Ip;

        _db.SaveChanges();
        TempData["success"] = "Success";

        MainModel model = await CreateMainModelAsync(mainModel.idAccount);
        return View("~/Views/Home/Main.cshtml", model);
    }



    //DELETE                DELETE                  DELETE
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteAccount(int? idAccount)
    {
        var accountToRemove = _db.Accounts.Find(idAccount);
        _db.Accounts.Remove(accountToRemove!);

        _db.SaveChanges();
        TempData["success"] = "Account deleted successfully";
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
        toPass.Ip = _db.Accounts.Single(c => c.id == toPass.idAccount).Ip;
        toPass.NumberPhoto = _db.Accounts.Single(c => c.id == toPass.idAccount).NumberPhoto;
        toPass.Connection = await ConnectionApi(toPass.Ip);

        return toPass;
    }




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

        byte[] responseData;
        if (response.Content.Headers.ContentType.MediaType == "application/octet-stream")
        {
            responseData = await response.Content.ReadAsByteArrayAsync();
        }
        else
        {
            responseData = null;
        }

        return responseData;
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



}