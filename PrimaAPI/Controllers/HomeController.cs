using Microsoft.AspNetCore.Mvc;
using ProgettoLogin.Data;
using ProgettoLogin.Models;
using System.Diagnostics;


namespace ProgettoLogin.Controllers;

public class HomeController : Controller
{

    private readonly ILogger<HomeController> _logger;

    private readonly ApplicationDbContext _db;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext db)
    {
        _logger = logger;                               //Per caricare la prima pagina
        _db = db;                                       //Per il database
    }
    

    public IActionResult Login()
    {
        IEnumerable<Account> objAccountList = _db.Accounts;
        return View();
    }


    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
    



    //LOGIN                             LOGIN                           LOGIN
    public IActionResult LoginAction(int? id)
    {
        if (id == null || id == 0)
        {
            return NotFound();
    
        }
        var accountFromDb = _db.Accounts.Find(id);            


        if (accountFromDb == null)
        {
            return NotFound();
        }
    
        return View(accountFromDb);
    }

    //private Account accountOfEmail;

    //POST action method
    [HttpPost]
    [ValidateAntiForgeryToken]      //To help and prevent the cross site request forgery attack
    public IActionResult LoginActionPost(Account accountObj)
    {
        Account accountOfEmail = new Account();
        MainModel toPass = new MainModel();

        if (accountObj.Email == " ")
        {
            ModelState.AddModelError("Name", "Error with your password");
        }
        
        if (ModelState.IsValid)     //per evitare l'eccezione in cui quancuno non scriva niente 
        {
            bool b_Login = false;
    
    
            foreach (var accountNow in _db.Accounts)
            {
                if (accountNow.Email == accountObj.Email && accountNow.Pass == accountObj.Pass)
                {
                    accountOfEmail = accountNow;
                    b_Login = true;
                }
            }

            if (b_Login)
            {
                toPass.idAccount = accountOfEmail.id;
                toPass.Email = _db.Accounts.Single(c => c.id == toPass.idAccount).Email!;

                foreach (var siteNow in _db.AccountXSites)
                {
                    if(siteNow.idAccount == accountOfEmail.id)
                    {
                        try
                        {
                            toPass.DateRecording!.Add(siteNow.DateRecording);
                            toPass.Name!.Add(_db.Sites.Single(c => c.id == siteNow.idSite).Name!);
                        }
                        catch
                        {
                            TempData["error"] = "One or more sites were not saved correctly";
                        }
                    }
                }
                try
                {
                        TempData["success"] = "Success";
                        return View("Main", toPass);
                }
                catch (NullReferenceException)
                {
                    TempData["error"] = "Error with your Account";
                }
                finally
                {
                    b_Login = false;
                }
    
            }
        }
        return View("Login");
    }
}





