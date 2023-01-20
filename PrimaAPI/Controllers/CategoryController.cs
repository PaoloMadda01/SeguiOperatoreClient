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

    //POST action method
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddSitePost(String? siteNowStr, Account account)
    {
        int indexNewSite;

        if(siteNowStr != null)
        {
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

            bool b_Save = true;
            foreach (var nameSite in _db.AccountXSites)
            {
                if (nameSite.idSite == indexNewSite && nameSite.idAccount == account.id)
                {
                    b_Save = false;
                    TempData["success"] = "This site is already saved in your account";
                    break;
                }
            }
            if (b_Save)
            {
                AccountXSite addNewSite = new AccountXSite();
                addNewSite.idAccount = account.id;
                addNewSite.idSite = indexNewSite;
                addNewSite.DateRecording = DateTime.Now;

                _db.AccountXSites.Add(addNewSite);
                _db.SaveChanges();
                TempData["success"] = "Site updated successfully";
            }
        }
        

        try
        {
            TempData["success"] = "Success";
            return View("~/Views/Home/Main.cshtml", OpenMainModel(account.id));
        }
        catch (NullReferenceException)
        {
            TempData["error"] = "Error with your Account";
        }
        return Redirect("~/Home/Login");
    }


    //DELETE                DELETE                  DELETE

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
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteSite(String? siteToDeleteString, int? idAccountNow)
    {
        var SiteObj = _db.Sites.Find(_db.Sites.Single(c => c.Name == siteToDeleteString).id!);
        var AccountXSitesObj = _db.AccountXSites.Find(_db.AccountXSites.Single(c => c.idSite == SiteObj!.id && c.idAccount == idAccountNow).id!);

        if (AccountXSitesObj == null)   return NotFound();

        if (AccountXSitesObj.idAccount == idAccountNow && AccountXSitesObj.idSite == SiteObj!.id)
        {
            _db.AccountXSites.Remove(AccountXSitesObj!);
            _db.SaveChanges();
            TempData["success"] = "Site deleted successfully";
        }
        try
        {
            TempData["success"] = "Success";
            return View("~/Views/Home/Main.cshtml", OpenMainModel(AccountXSitesObj!.idAccount));
        }
        catch (NullReferenceException)
        {
            TempData["error"] = "Error with your Account";
        }

        return Redirect("~/Home/Login");
    }



    //METODO PER VISUALIZZARE LA PAGINA MAIN
    [HttpPost]
    [ValidateAntiForgeryToken]
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