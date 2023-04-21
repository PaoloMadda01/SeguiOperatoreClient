using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace ProgettoLogin.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserControllerApi : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly string _pepper = Environment.GetEnvironmentVariable("pepperString"); //Variabile d'ambiente per le password

        public UserControllerApi(ApplicationDbContext db)
        {
            _db = db;
        }


        //TEST STATUS API
        [HttpGet("status")]
        public IActionResult Status()
        {
            return Ok("Ok");
        }


        [HttpGet("list-account")]
        public IActionResult List()     //Restituisce la lista degli account
        {
            List<String> listAccount = new List<String>();
            foreach (var accountNow in _db.Accounts)
            {
                listAccount.Add(accountNow.Email);
            }
            

            return Ok(listAccount);
        }



        [HttpGet("getbyid")]
        public IActionResult GetById(int id) //Restituisce l'account in base all'id
        {
            var account = _db.Accounts.SingleOrDefault(c => c.id == id);
            if (account == null) return BadRequest();
            String accountEmail = account.Email;

            return Ok(accountEmail);
        }


        [HttpGet("account-site")]
        public IActionResult AccountSite() //Per ogni utente restituisce tutti i siti
        {
            List<(string account, List<string> sites)> accountSitesList = new List<(string account, List<string> sites)>();

            foreach (var account in _db.Accounts)
            {
                var siteIds = _db.AccountXSites
                    .Where(axs => axs.idAccount == account.id && axs.idSite != null)
                    .Select(axs => axs.idSite);

                var sites = _db.Sites
                    .Where(s => siteIds.Contains(s.id))
                    .Select(s => s.Url)
                    .ToList();

                accountSitesList.Add((Account: account.Email, Siti: sites));
            }

            string jsonString = JsonConvert.SerializeObject(accountSitesList, Formatting.Indented);

            return Ok(jsonString);
        }


        [HttpGet("top-site")]
        public IActionResult TopSavedSites(int numberOfSites)
        {
            List<String?> allSites = new();
            int allSitesCount = _db.AccountXSites.Where(x => x.idSite != null).Count();

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

            String stringJson = JsonConvert.SerializeObject(topSavedSites, Formatting.Indented);

            return Ok(stringJson);
        }



        //PAGINA ADMIN
        //METODO PER DISEGNARE IL GRAFICO PER MOSTRARE QUANDO GLI UTENTI REGISTRANO UN NUOVO ACCOUNT
        //X:TEMPO  -  Y:NUMERO UTENTI 
        //UTILIZZO LA LIBRERIA Chart.js
        [HttpGet("when-new")]
        public IActionResult WheneNewAccount(int numberOfDays)
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
            String stringJson = JsonConvert.SerializeObject(chartData, Formatting.Indented);

            return Ok(stringJson);
        }
    }
}