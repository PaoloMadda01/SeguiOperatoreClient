using Microsoft.AspNetCore.Mvc;
using ProgettoLogin.Data;
using ProgettoLogin.Models;
using System.Linq;
using System.Security.Principal;

namespace ProgettoLogin.Controllers;
public class CategoryController : Controller
{
    private readonly ApplicationDbContext _db;

    public CategoryController(ApplicationDbContext db)
    {
        _db = db;
    }


    //GET action method
    public IActionResult Create()
    {
        return View();
    }

    //POST action method
    [HttpPost]
    [ValidateAntiForgeryToken]      //To help and prevent the cross site request forgery attack
    public IActionResult Create(Account account)
    {
        if (account.Email == account.Pass)
        {
            ModelState.AddModelError("Email", "Error with your password");
        }

        foreach (var Index in _db.Accounts)
        {
            if (account.Email == Index.Email)
            {
                ModelState.AddModelError("Email", "This Email already exists");
                TempData["error"] = "Try another email";
                return View(account);
            }
        }

        if (ModelState.IsValid)
        {
            _db.Accounts.Add(account);
            _db.SaveChanges();

            TempData["success"] = "Account created successfully";

            Account accountObj = new Account();
            accountObj.id = account.id;

            return View("AddSite", accountObj);
        }
        return Redirect("~/Category/Create");
    }


   // //CHOICE            CHOICE                  CHOICE                  CHOICE
   // public IActionResult Choice(int? id1, int? id2)
   // {
   //     if (id1 == null || id1 == 0)
   //     {
   //         return NotFound();
   //
   //     }
   //     if (id2 == null || id2 == 0)
   //     {
   //         return NotFound();
   //
   //     }
   //     var categoryFromDb1 = _db.Accounts.Find(id1);
   //     var categoryFromDb2 = _db.AccountXSites.Find(id2);
   //
   //     if (categoryFromDb1 == null)
   //     {
   //         return NotFound();
   //     }
   //     if (categoryFromDb2 == null)
   //     {
   //         return NotFound();
   //     }
   //
   //     var toPass = Tuple.Create(categoryFromDb1, categoryFromDb2);
   //     return Redirect("~/Category/Choice");
   // }
   //
   //
   // //POST action method
   // [HttpPost]
   // [ValidateAntiForgeryToken]
   // public IActionResult ChoicePost(String siteNowStr, Account account)
   // {
   //     if (_db.Sites.Find(siteNowStr) == null)
   //     {
   //         Site siteNowObj = new Site();
   //         siteNowObj.Name = siteNowStr;
   //         _db.Sites.Add(siteNowObj);
   //     }
   //
   //     foreach (var nameSite in _db.AccountXSites)
   //     {
   //         if (_db.AccountXSites.Find(siteNowStr) != null && _db.AccountXSites.Find(siteNowStr) != null)
   //         {
   //             AccountXSite addNewSite = new AccountXSite();
   //             addNewSite.idAccount = account.id;
   //             addNewSite.idSite = _db.Sites.Single(c => c.Name == siteNowStr).id;
   //             addNewSite.DateRecording = DateTime.Now;
   //
   //             _db.AccountXSites.Add(addNewSite);
   //             _db.SaveChanges();
   //             TempData["success"] = "Site updated successfully";
   //         }
   //         else
   //         {
   //             TempData["success"] = "The site already exists in your account";
   //         }
   //     }
   //
   //     return Redirect("~/Category/Choice");
   // }



    //ADDSITE            ADDSITE                  ADDSITE                  
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddSite(int? idAccount)
    {
        if (idAccount == null || idAccount == 0)
        {
            return NotFound();
    
        }
        var categoryFromDb = _db.Accounts.Find(idAccount);
    
        if (categoryFromDb == null)
        {
            return NotFound();
        }
        return View(categoryFromDb);
    }

    //private Account account;
    //POST action method
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddSitePost(String? siteNowStr, Account account)
    {
        int indexNewSite;
        try
        {
            indexNewSite = _db.Sites.Single(c => c.Name == siteNowStr).id;

        }
        catch
        {
            Site siteNowObj = new Site();
            siteNowObj.Name = siteNowStr;
            _db.Sites.Add(siteNowObj);
            _db.SaveChanges();
            indexNewSite = _db.Sites.Single(c => c.Name == siteNowStr).id;
        }
        

        foreach (var nameSite in _db.AccountXSites)
        {
            if (nameSite.idSite != indexNewSite && nameSite.id != account.id)
            {
                AccountXSite addNewSite = new AccountXSite();
                addNewSite.idAccount = account.id;
                addNewSite.idSite = indexNewSite;
                addNewSite.DateRecording = DateTime.Now;

                _db.AccountXSites.Add(addNewSite);
                TempData["success"] = "Site updated successfully";
                break;
            }
        }
        _db.SaveChanges();
        TempData["success"] = "Site updated successfully";

        MainModel toPass = new MainModel();

        toPass.idAccount = account.id;
        toPass.Email = _db.Accounts.Single(c => c.id == toPass.idAccount).Email!;

        foreach (var siteNow in _db.AccountXSites)
        {
            if (siteNow.idAccount == account.id)
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
            //if (_db.AccountXSites.Single(c => c.idAccount == toPass.idAccount).id != 0)     //Controlla che in AccountXSites sia salvato correttamente idAccount quando  stato fatto l'account
            //{
            TempData["success"] = "Success";
            return View("~/Views/Home/Main", toPass);
            //}
        }
        catch (NullReferenceException)
        {
            TempData["error"] = "Error with your Account";
        }
        return Redirect("~/Home/Login");
    }


    //DELETE                DELETE                  DELETE


    //POST action method
    [HttpPost]
    [ValidateAntiForgeryToken]      //To help and prevent the cross site request forgery attack
    public IActionResult DeleteAccount(int? idAccount)
    {
        var accountToRemove = _db.Accounts.Find(idAccount);
        _db.Accounts.Remove(accountToRemove!);

        foreach (var accountXSiteToRemove in _db.AccountXSites)
        {
            if (accountXSiteToRemove.idAccount == idAccount)
            {
                try
                {
                    _db.AccountXSites.Remove(accountXSiteToRemove!);
                }
                catch
                {
                    TempData["error"] = "One or more sites were not removed correctly";
                }
            }
        }

        _db.SaveChanges();
        TempData["success"] = "Account deleted successfully";
        return Redirect("~/Home/Login");
    }



    //DELETE SITE
    //public IActionResult DeleteSite(int? idAccountXSite)
    //{
    //    if (idAccountXSite == null || idAccountXSite == 0)
    //    {
    //        return NotFound();
    //
    //    }
    //    var rowFromDb = _db.AccountXSites.Find(idAccountXSite);
    //
    //    if (rowFromDb == null)
    //    {
    //        return NotFound();
    //    }
    //
    //    return View(rowFromDb);
    //}

    //POST action method
    [HttpPost]
    [ValidateAntiForgeryToken]      //To help and prevent the cross site request forgery attack
    public IActionResult DeleteSite(int? idSiteToDelete, MainModel? mainModelObj)
    {
        //mainModelObj e idSiteToDelete sono 0 da sistemare

        var SiteObj = _db.Sites.Find(_db.Sites.Single(c => c.Name == mainModelObj!.Name[idSiteToDelete!.Value]).id!);       //.Value perchè int? --> int
        var AccountXSitesObj = _db.AccountXSites.Find(_db.AccountXSites.Single(c => c.idSite == SiteObj!.id && c.idAccount== mainModelObj!.idAccount).id!);



        if (AccountXSitesObj == null)   return NotFound();

        if (AccountXSitesObj.idAccount == idSiteToDelete && AccountXSitesObj.idAccount == mainModelObj!.idAccount)
        {
            _db.AccountXSites.Remove(AccountXSitesObj!);        //per aggiungere/aggiornare i dati di obj nel database
            _db.SaveChanges();              //per salvare tutte le modifiche
            TempData["success"] = "Site deleted successfully";          //Salvato questo messaggio nel tempData con la key "success"

        }
        try
        {
            TempData["success"] = "Success";
            return View("~/Home/Main", OpenMainModel(AccountXSitesObj!.idAccount));
        }
        catch (NullReferenceException)
        {
            TempData["error"] = "Error with your Account";
        }

        return Redirect("~/Home/Login");

    }



    public MainModel OpenMainModel(int idAccountNow)
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
                    toPass.Name!.Add(_db.Sites.Single(c => c.id == siteNow.idSite).Name!);
                }
                catch
                {
                    TempData["error"] = "One or more sites were not saved correctly";
                }
            }
        }
        return toPass;
    }
}