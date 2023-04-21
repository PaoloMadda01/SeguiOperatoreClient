using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace ProgettoLogin.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EditControllerApi : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly string _pepper = Environment.GetEnvironmentVariable("pepperString"); //Variabile d'ambiente per le password

        public EditControllerApi(ApplicationDbContext db)
        {
            _db = db;
        }

        //POST action method
        [HttpPost("create-account")]
        public IActionResult Create(Login login)
        {
            Account account = new Account();
            account.Email = login.Email;

            if (account.Email == login.Pass)
            {
                ModelState.AddModelError("Email", "Error with your password");
            }

            foreach (var accountNow in _db.Accounts)
            {
                if (account.Email == accountNow.Email)
                {
                    ModelState.AddModelError("Email", "This Email already exists");
                    return BadRequest(ModelState);
                }
            }

            if (ModelState.IsValid)
            {
                string salt = GenerateSalt();
                account.PasswordSalt = Encoding.UTF8.GetBytes(salt);
                string passwordHash = ComputeHash(login.Pass, account.PasswordSalt.ToString(), _pepper, 5000);
                account.PasswordHash = Encoding.UTF8.GetBytes(passwordHash);

                _db.Accounts.Add(account);
                _db.SaveChanges();

                return StatusCode(StatusCodes.Status201Created, account);
            }
            return BadRequest(ModelState);
        }


        //CHANGE PASSWORD       CHANGE PASSWORD         CHANGE PASSWORD         CHANGE PASSWORD
        [HttpPatch("change-password")]
        public IActionResult ChangePass(int idAccount, String PassNow, String PassNew, String PassNewR)
        {
            bool b_tempData = false;

            foreach (var accountNow in _db.Accounts)
            {
                if (idAccount == accountNow.id && PassNew == PassNewR)
                {
                    string passwordHash = ComputeHash(PassNow, accountNow.PasswordSalt.ToString(), _pepper, 5000);
                    byte[] passwordHashByte = Encoding.UTF8.GetBytes(passwordHash);
                    if (passwordHashByte.SequenceEqual(accountNow.PasswordHash))        //Per fare il controllo dei valori tra due array
                    {
                        string saltNew = GenerateSalt();
                        accountNow.PasswordSalt = Encoding.UTF8.GetBytes(saltNew);
                        string passwordHashNew = ComputeHash(PassNew, accountNow.PasswordSalt.ToString(), _pepper, 5000);    //Stringa che si vuole crittografare,
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
                //TempData["success"] = "Success";
            }
            else
            {
                //TempData["error"] = "Error";
            }
        
            return Ok(CreateMainModel(idAccount, _db));
        }
        
        [HttpPost("add-site")]
        public IActionResult AddSitePost(int idAccount, String siteNowStr)
        {
            int indexNewSite;
            var accountNow = _db.Accounts.Find(idAccount);
        
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
                        return BadRequest(accountNow);
                    }
                    indexNewSite = _db.Sites.Single(c => c.Url == siteNowStr).id;
                }
        
                bool b_AddNewSite = true;
                foreach (var nameSite in _db.AccountXSites)
                {
                    if (nameSite.idSite == indexNewSite && nameSite.idAccount == idAccount)
                    {
                        b_AddNewSite = false;
                        break;
                    }
                }
                if (b_AddNewSite)
                {
                    AccountXSite addNewSite = new AccountXSite();
                    addNewSite.idAccount = idAccount;
                    addNewSite.idSite = indexNewSite;
                    addNewSite.DateRecording = DateTime.Now;
        
                    _db.AccountXSites.Add(addNewSite);
                    _db.SaveChanges();
                }
                return Ok(CreateMainModel(idAccount, _db));
            }
            else
            {
                // URL non valido
                return BadRequest(accountNow);
            }
        
            return BadRequest(false);
        }


        //DELETE  ACCOUNT              DELETE  ACCOUNT                    DELETE  ACCOUNT
        [HttpDelete("delete-account")]
        public IActionResult DeleteAccount(int? idAccount)
        {
            var accountToRemove = _db.Accounts.Find(idAccount);
            if (accountToRemove != null)
            {
                _db.Accounts.Remove(accountToRemove!);

                foreach (var accountXSiteToRemove in _db.AccountXSites)
                {
                    if (accountXSiteToRemove.idAccount == idAccount) _db.AccountXSites.Remove(accountXSiteToRemove!);
                }

                _db.SaveChanges();
                return Ok("Done");
            }
            else
            {
                return BadRequest(false);
            }

        }



        //DELETE SITE            DELETE SITE             DELETE SITE
        [HttpDelete("delete-site")]
        public IActionResult DeleteSite(int? idAccount, string? siteToDeleteString)
        {
            // Controllo se gli input sono nulli o vuoti
            if (idAccount == null || string.IsNullOrEmpty(siteToDeleteString))
            {
                return BadRequest(false);
            }

            try
            {
                // Cerco il sito da eliminare
                var siteToDelete = _db.Sites.SingleOrDefault(s => s.Url == siteToDeleteString);
                if (siteToDelete == null)
                {
                    return BadRequest("Il sito non esiste");
                }

                // Controllo se l'utente è autorizzato ad eliminare il sito
                var accountXSite = _db.AccountXSites.SingleOrDefault(a => a.idSite == siteToDelete.id && a.idAccount == idAccount);
                if (accountXSite == null)
                {
                    return BadRequest("Non sei autorizzato ad eliminare questo sito");
                }

                // Elimino il collegamento tra utente e sito
                _db.AccountXSites.Remove(accountXSite);
                _db.SaveChanges();

                // Restituisco la vista principale
                var mainModel = CreateMainModel(accountXSite.id, _db);
                return Ok(mainModel);
            }
            catch (Exception ex)
            {
                // Gestione delle eccezioni
                return StatusCode(500, "Si è verificato un errore durante l'eliminazione del sito: " + ex.Message);
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
        
        //METODO PER VISUALIZZARE LA PAGINA MAIN
        public static MainModel CreateMainModel(int idAccountNow, ApplicationDbContext _db)
        {
            MainModel toPass = new MainModel();

            toPass.idAccount = idAccountNow;
            try
            {
                var account = _db.Accounts.SingleOrDefault(c => c.id == idAccountNow);
                if (account != null)
                {
                    toPass.Email = account.Email;
                }
                else
                {
                    return null;
                }
            }
            catch (NullReferenceException e)
            {
                return null;
            }

            foreach (var siteNow in _db.AccountXSites)
            {
                if (siteNow.idAccount == idAccountNow)
                {
                    var siteUrl = _db.Sites.SingleOrDefault(c => c.id == siteNow.idSite).Url;
                    if (siteUrl != null)
                    {
                        toPass.DateRecording.Add(siteNow.DateRecording);
                        toPass.Name?.Add(siteUrl);
                    }
                    if (siteUrl == null)
                    {
                        toPass.DateRecording.Add(DateTime.Now);
                        toPass.Name?.Add("No site saved");
                    }
                }
            }
            return toPass;
        }
    }
}