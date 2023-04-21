# ProgettoLogin

In questo Progetto è stato sviluppato un sito web che permette agli utenti di gestire un elenco da loro creato di siti web. L’utente dovrà inizialmente creare un account inserendo email e password. Verrà poi richiesto di aggiunge il primo sito web all’account. Verrà poi visualizzata la pagina in cui potrà gestire la sua lista di siti web. Potrà aggiungerne di nuovi, toglierne oppure eliminare l’account. Nella pagina iniziale verrà richiesto l’email e la password per poter accedere all’account e gestire i propri siti web registrati. Inserendo delle credenziali specifiche è possibile visualizzare la pagina dell’admin dove l’admin potrà visualizzare alcune informazioni sul sito web come: un grafico che raffigura il numero di account creati nei diversi giorni oppure i sitiweb che vengono aggiunti maggiormente negli account.

Database DBLogin

Per poter salvare ed utilizzare in modo corretto le informaioni è stato deciso di dividerle in tre tabelle: “Account”, “AccountXSite” e “Site”. Nella tabella “Account verranno salvate tutte le informazioni riguardanti l’account. Nella tabella “Site” verranno salvati gli url dei siti web. La tabella “AccountXSite” permette di mettere in relazioni, nello specifico una relazione molti a molti, le tabelle descritte precedentemente.

![image](https://user-images.githubusercontent.com/109733062/233655028-2c742073-b964-4222-a1a6-dcdd73c27315.png)

![image](https://user-images.githubusercontent.com/109733062/233655228-63d7d1b4-cea7-48a0-afe7-d0c5af32b794.png)


Metodi
Il progetto è suddiviso in tre controller: “HomeController” e “EditController”. Segue una breve spiegazione dei metodi utilizzati.
HomeController

	Login: Questo metodo viene chiamato quando il client vuole mostrare la view iniziale del login nella quale è permesso creare un nuovo account o eseguire l’accesso. Viene chiamato di defalt quando il servizio, ProgettoLogin, viene eseguito.  

	LoginActionPostAsync: Viene chiamato quando client desidera accedere all’account dopo aver inserito l’email e l’account. Se l’email inserita corrisponde ad un account esistente nel database e la password è corretta viene chiamato il metodo “CreateAdminModel” e successivamente verrà visualizzata la view “Main”. Se l’email e la password inserite corrispondono all’account dell’Admin allora verrà chiamato il metodo “CreateAdminModel” e successivamente verrà visualizzata la view “Admin”. É un metodo asincrono perchè dovrà aspettare risposta dall’Api “TimeZoneApi”.

	CameraLogin: Qusto metodo viene utilizzato per eseguire il login in modo alternativo dal scrivere l’email e la password. In questo caso viene eseguito il login inserendo l’email e facendo una foto. Questa foto verrà utilizzata dall’Api “OpenCVApi” per fare il confronto con la foto che è stata salvata nel database. Verrà chiamato il metodo “ProcessImageApi” che invia all’Api la foto appena eseguita e la foto salvata nel database quando è stato creato l’account. Se l’Api restituisce uno score superiore a 0,7 verrà dato il permesso al login e verrà chiamato il metodo “CreateMainModelAsync”.
EditController

	Create: Viene utilizzato per creare un nuovo account, verrà chiesta l’email, la password e la città di provenienza. Se l’email non è già stata utilizzata da un altro account e la password rispetta gli attributi, verrà creato l’account.

	AddPhotoAsync: Viene utilizzato per aggiungere la foto del viso nel database per utilizzare il login facciale. É un metodo asincrono perchè dovrà aspettare risposta dall’Api “TimeZoneApi”.

	ChangePassAsync: Utilizzato per modificare la password. Viene richiesta la password precendente e la nuova scritta due volte. Se la password precendente è corretta e le nuove password scritte due volte sono corrette, verrà effettivamete cambiata. In caso contrario verrà visualizzato un messaggio di errore.

	AddSite: Utilizzato per visualizzare la view AddSite passando l’idAccount.

	AddSitePostAsync: Utilizzato per aggiungere un nuovo sito. Se il testo inserito è scritto correttamente ed è effettivamente un sito allora potrà essere aggiunto nel database nella tabella “AccountXSite”. In questo metodo verrà controllato se il sito è già presente nella tabella “Site”, se non lo è viene aggiunto con un nuovo id. È asincrono perchè dopo che il sito è stato aggiunto verrà visualizzata la view “Main” e dovrà aspettare la risposta dell’Api “TimeZoneApi”.

	DeleteAccount: Utilizzato per eliminare l’account.

	DeleteSite: Utilizzato per cancellare il riferimento del sito dall’account. Verrà rimossa la riga contenente il sito dell’account che ha fatto la richiesta dalla tabella “AccountXSite” del database. Non verrà cancellato il sito dalla tabella “Site”, altrimenti verrebbe rimosso anche da altri account che non l’hanno richiesto.
Gestione Main view

	CreateMainModelAsync: Utilizzato per creare il model che verrà utilizzato dal front-end per visualizzare correttamente la view “Main”. É asincrono perchè dovrà aspettare la risposta dell’Api “TimeZoneApi”.
Gestione Admin

	CreateAdminModel: Metodo per creare correttamente un’istanza del model “AdminModel”. Questo metodo richiama i metodi “TopSavedSites” e “DrawTimeXSitesGraphic”. Restituisce l’istanza di “AdminModel”.

	TopSavedSites: Crea una lista di siti e li classifica in base alla percentuale di quanti account hanno aggiunto quel sito nella tabella “AccountXSite” del database. Il metodo inizialmente resituisce una lista con il nome dei siti e la percentuale.

	DrawTimeXSitesGraphic:Utilizzato per disegnare, con la libreria “Chart.js”, il grafico che mostra quando gli utenti hanno creato un nuovo account. In ingresso il metodo richiede quanti giorni deve mostrare il grafico. In base al numero viene individuato il giorno di partenza, lasciando sempre come ultimo giorno il giorno attuale. Nell’asse delle ascisse del grafico verranno visualizzati gli intervalli di giorni. Nell’asse delle ordinate verranno visualizzato il numero di account creato in quel intervallo.

Gestione password

	GenerateSalt: Viene utilizzato per criptare o decriptare la password. GenerateSalt è un metodo per creare “Salt”, viene creato un numero random e poi viene convertito in una stringa in   base 64

	ComputeHash: È un metodo ricorsivo che viene chiamato 5000 volte che serve per generare l’hash.
Api esterne

	ProcessImageApi: Viene chiamato quando si vuole effettuare il login con il viso. In ingresso richiede la foto dal database e la foto fatta. Invia una richiesta di tipo post e aspetta la risposta dall’Api. La risposta è lo score e si riferisce a quanto le immagini si assomigliano. Se lo score è superiore a 0,7 allora il metodo restituisce un true, altrimenti un false.

	TimeZoneApiMethodAsync: È il metodo che permette di connettersi all’Api “weatherapi-com” oline. Viene chiamato l’Uri che si riferisce all’Api e al servizio timezone dell’Api. All’Uri viene aggiunta la città di cui si vuole sapere la timezone. Nel body della richiesta Get è presente la chiave di accesso. Il metodo restituisce le informazioni date dall’Api.


Model e Attributi

	Account: utilizzato quando si vogliono utilizzare le informazioni base di un account. 

	AccountXSite: utilizzato quando si vuole creare una nuova riga nella tabella “AccountXSites” per aggiungere un sito in riferimento all’account corrispondente.

	Admin: utilizzato ogni volta che si vuole visualizzare correttamente la view “Admin”.

	Login: utilizzato quando si vuole eseguire l’accesso o creare un nuovo account, ogni istanza di questo model deve possedere: l’email e la password. Il client deve inserire un’email valida con più di 5 caratteri e una password non più corta dei 5 caratteri.

	Main: utilizzato ogni volta che si vuole visualizzare correttamente la view “Main”.

	Site: utilizzato quando si vuole creare o aggiungere un sito.


FRONT-END
Il fornt-end è stato sviluppato con HTML, CSS e JavaScript. Per semplificare e velocizzare è stato scelto di utilizzare un tema gratuito per bootstrap, un framework per sviluppo web. In particolare è stato scelto dal sito web “bootswatch” (https://bootswatch.com/) che fornisce diversi temi gratuiti.

Views Home

	Login: È la view iniziale che viene utilizzata per eseguire il login o per creare un nuovo account.

	Main: Viene utilizzata per gestire la lista degli account, in essa è permesso aggiungere, togliere siti web o eliminare l’account. È poi presente il pulsante 
“Camera” che permetterà di scattare una foto per il login con riconoscimento facciale che verrà spiegato successivamente. Verrano poi mostrate altre informazioni come l’email e informazioni riguardanti la città scelta dall’utente in fase di registrazione.

	Admin: Questa view viene dedicata all’admin e può essere visualizzata solamente conoscendo la password corretta. In essa verrà visualizzato il grafico sviluppato con la libreria Chart.js che mostra il numero di account registrati in un periodo di tempo. Il periodo di tempo può essere modificato in base alle esigenze dell’admin e il grafico verrà aggiornato in base al periodo scelto. Verrà anche visualizzata la lista, in ordine di popolarità, dei siti web più comuni tra gli account e le relative percentuali sul totale. È possibile scegliere il siti che verranno mostrati.

Views Edit

	Create: Viene utilizzata per creare un nuovo account, verrà richiesta l’email, la password e la città di nascita.

	AddSite: Permette di aggiungere un nuovo sito alla lista.
Modal box
Una modal box è una finestra di dialogo o popup che viene visualizzata sopra la pagina corrente. Viene utilizzata per mostrare richieste, informazioni, avvisi, errori. Per attivare una modal box è necessario utilizzare una funzione JavaScript che seleziona l’elemento HTML che rappresenta la modal box e modifica il suo stile di visualizzazione per renderlo visibile. Per nascondere la modal box, si può utilizzare una funzione simile che cambia il suo stile di visualizzazione in modo da nasconderlo.

	DeleteSite: In questa model box verrà attivata quando verrà premuto il pulsante “Delete Site” e chiede la conferma all’utente di togliere dalla lista il sito web selezionato.

	ChangePass: Verrà attivata quando verrà premuto il pulsante “Change Password”. È utilizzata per modificare la password dell’account. In essa verrà richiesta la password che si intende cambiare e la nuova password.

	Camera: Verrà attivata una model box che permette di visualizzare il video della webcam e, premendo il pulsante “Take Photo”, verrà scattata una foto che verrà salvata o aggiornata nel database.

	DeleteAccount: Utilizzata per chiedere conferma se l’utente vuole veramente eliminare l’account.
