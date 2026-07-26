# 🚀 Guida alla pubblicazione su Google Play — passo per passo

> Obiettivo: portare Bolletta Analyzer dallo stato attuale (funzionante in locale)
> a un'app **pubblicata su Google Play** e **funzionante al 100%** sui telefoni degli utenti.
>
> Tempo stimato di lavoro effettivo: **3–5 ore**. Tempi di attesa esterni (verifiche
> Google): vedi il riquadro qui sotto, **leggilo prima di tutto**.

---

## ⚠️ Prima di tutto: la verità sui tempi di Google

Due vincoli **indipendenti da noi** che devi conoscere subito:

1. **Account sviluppatore**: la registrazione costa $25 (una tantum) e richiede una
   **verifica d'identità** che può durare da poche ore a 2 giorni.
2. **Account personali creati dopo nov. 2023**: prima di poter pubblicare in
   **produzione** (visibile a tutti sul Play Store), Google impone un periodo di
   **test chiuso con almeno 12 tester per 14 giorni consecutivi**.
   Gli account **aziendali/organizzazione** non hanno questo vincolo.

**Cosa è realistico per domani:**
- ✅ App **completa, firmata e caricata** su Play Console in **test interno**
  (installabile subito da te e da chi inviti via link — fino a 100 persone)
- ✅ Avvio del test chiuso con i tester
- ❌ Pubblicazione in produzione visibile a tutti (impossibile per un account
  personale nuovo: il vincolo dei 14 giorni non è aggirabile)

Il piano qui sotto ti porta al massimo ottenibile per domani: **app al 100%
funzionante + caricata in test interno**, con la produzione che scatta appena
i vincoli di Google lo consentono.

---

## 📋 La checklist completa (in ordine)

| # | Cosa | Dove | Tempo |
|---|------|------|-------|
| 1 | Database PostgreSQL gestito | Neon o Supabase (gratis) | 10 min |
| 2 | Backend pubblicato con HTTPS | Azure App Service / Render | 45 min |
| 3 | Chiavi segrete di produzione | variabili d'ambiente dell'host | 10 min |
| 4 | Client che punta all'API pubblica | `.env.production` | 5 min |
| 5 | Keystore di firma Android | il tuo PC (da **conservare per sempre**) | 10 min |
| 6 | Build AAB firmato | `dotnet publish` | 15 min |
| 7 | Account Play Console + scheda app | play.google.com/console | 1–2 h |
| 8 | Privacy policy pubblica | pagina web (basta GitHub Pages) | 30 min |
| 9 | Caricamento in test interno | Play Console | 15 min |

---

## 1️⃣ Database PostgreSQL (10 min)

Il backend supporta già PostgreSQL (basta configurarlo — nessuna modifica al codice).
SQLite **non va bene** in hosting cloud: il file sparisce a ogni riavvio del container.

**Opzione consigliata: [Neon](https://neon.tech)** (gratis, niente carta di credito):
1. Registrati → **Create project** → nome `bolletta-analyzer`, region `EU (Frankfurt)`
2. Dalla dashboard copia la **connection string** in formato .NET/Npgsql. Ha questa forma:
   ```
   Host=ep-xxx.eu-central-1.aws.neon.tech;Database=neondb;Username=xxx;Password=xxx;SSL Mode=Require
   ```
3. Conservala: la userai al passo 3 come `ConnectionStrings__Default`.

Alternativa equivalente: [Supabase](https://supabase.com) → Project Settings →
Database → Connection string (URI da convertire in formato chiave=valore, o usa
il "Session pooler").

> Lo schema del database viene creato **automaticamente al primo avvio** del backend
> (`EnsureCreated`). In produzione il **seed demo non gira**: il primo utente si crea
> registrandosi dall'app.

## 2️⃣ Backend pubblico con HTTPS (45 min)

L'app sui telefoni degli utenti deve chiamare un'API **raggiungibile da internet in
HTTPS**. Il tuo PC non lo è. Due strade — scegline **una**:

### Opzione A — Azure App Service (consigliata per .NET, piano F1 gratuito)

1. Account su [portal.azure.com](https://portal.azure.com) (serve carta, il piano F1 non addebita)
2. **Crea risorsa → Web App**:
   - Nome: `bolletta-api` (l'URL sarà `https://bolletta-api.azurewebsites.net`)
   - Publish: **Code** · Runtime: **.NET 9 (STS)** · OS: **Linux** · Piano: **F1 Free**
3. Pubblica il codice (dalla cartella del repo sul tuo PC):
   ```powershell
   # una tantum: installa Azure CLI da https://aka.ms/installazurecliwindows, poi:
   az login
   cd src\Backend\BollettaAnalyzer.Api
   dotnet publish -c Release -o publish
   Compress-Archive publish\* publish.zip -Force
   az webapp deploy --resource-group <IL-TUO-RG> --name bolletta-api --src-path publish.zip --type zip
   ```
   (In alternativa: click destro sul progetto Api in Visual Studio → **Pubblica** → Azure)

### Opzione B — Render.com con Docker (gratis, più semplice, si spegne dopo 15 min di inattività)

Nel repo c'è già il **`Dockerfile`** pronto:
1. Account su [render.com](https://render.com) → **New → Web Service** → collega il repo GitHub
2. Render rileva il Dockerfile da solo. Piano: **Free**
3. L'URL sarà `https://bolletta-api-xxx.onrender.com`

> Il piano free di Render "addormenta" il servizio: la prima richiesta dopo una pausa
> impiega ~40 secondi. Accettabile per il test; per la produzione passa al piano da $7.

### 🧾 Nota OCR in produzione
Il provider `Local` legge i **PDF digitali** senza dipendenze (PdfPig, incluso).
L'OCR delle **foto** richiede Tesseract nativo: sull'host Linux non c'è, quindi le foto
daranno un errore chiaro (422) senza rompere nulla. Per la v1 va bene così; quando vuoi
le foto, attiva il provider `Azure` (Document Intelligence, tier gratuito F0: 500
pagine/mese) impostando `Ocr__Provider=Azure`, `Ocr__Azure__Endpoint`, `Ocr__Azure__ApiKey`.

## 3️⃣ Chiavi segrete di produzione (10 min)

Il backend **si rifiuta di partire** senza queste chiavi (per progetto: mai chiavi di
default in produzione). Generale sul tuo PC:

```powershell
# Chiave di firma JWT (stringa casuale robusta)
-join ((48..57)+(65..90)+(97..122) | Get-Random -Count 64 | % {[char]$_})

# Chiave AES-256 per i documenti cifrati (Base64, 32 byte)
[Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
```

Impostale come **variabili d'ambiente** sull'host (Azure: *Impostazioni → Variabili
di ambiente*; Render: *Environment*). I doppi underscore sostituiscono i `:` della
configurazione .NET:

| Variabile | Valore |
|---|---|
| `Jwt__Key` | la stringa da 64 caratteri |
| `Encryption__Key` | la chiave Base64 |
| `Database__Provider` | `Postgres` |
| `ConnectionStrings__Default` | la connection string di Neon (passo 1) |
| `ASPNETCORE_ENVIRONMENT` | `Production` |

⚠️ **Conserva `Encryption__Key` in un posto sicuro** (password manager): se la perdi,
i PDF dei contratti già caricati diventano illeggibili per sempre.
⚠️ **Non committare mai** questi valori nel repository.

**Verifica**: apri `https://TUO-BACKEND/.../` (la radice) nel browser →
deve rispondere `{"app":"Bolletta Analyzer API","status":"ok"}`.
La prima risposta crea anche lo schema su Postgres.

> Il CORS è già pronto: `appsettings.json` include l'origine `https://0.0.0.1`,
> che è l'origine interna dell'HybridWebView MAUI. Senza, l'app pubblicata non
> potrebbe chiamare le API.

## 4️⃣ Client → API pubblica (5 min)

Apri **`src/Frontend/BollettaAnalyzer.Maui/ClientApp/.env.production`** e imposta
l'URL del passo 2:

```env
VITE_API_BASE_URL=https://bolletta-api.azurewebsites.net/api
VITE_USE_MOCK=false
```

Questo file viene letto **solo** dal build di Release; il debug continua a usare
`10.0.2.2:5080` automaticamente.

**Test rapido end-to-end prima di firmare**: avvia il debug sull'emulatore con
`VITE_API_BASE_URL` puntato all'URL pubblico anche in `.env.local` → registra un
utente → se funziona, sei pronto. Poi **cancella `.env.local`**.

## 5️⃣ Keystore di firma (10 min) — ⚠️ da conservare PER SEMPRE

Ogni APK/AAB va firmato. **Se perdi il keystore non potrai più aggiornare l'app**
(dovresti ripubblicarla con un altro package name, perdendo utenti e recensioni).

```powershell
cd C:\Users\ivang\Documents\Personale   # FUORI dal repository!
& "$env:ProgramFiles\Microsoft\jdk-*\bin\keytool.exe" -genkeypair -v `
  -keystore bolletta-release.keystore -alias bolletta `
  -keyalg RSA -keysize 2048 -validity 10000
```
(Se il percorso del JDK non esiste, cerca `keytool.exe` dentro
`C:\Program Files\Microsoft\` o `C:\Program Files\Android\`.)

Ti chiede una password e i dati anagrafici (bastano nome e IT come paese).

📦 **Backup immediato** del file `.keystore` + password in un password manager e
su un secondo supporto. Non metterlo mai nel repository.

## 6️⃣ Build AAB firmato (15 min)

```powershell
cd C:\Users\ivang\Documents\Personale\Bill-Analyzer\src\Frontend\BollettaAnalyzer.Maui

dotnet publish -f net9.0-android -c Release `
  -p:AndroidKeyStore=true `
  -p:AndroidSigningKeyStore=C:\Users\ivang\Documents\Personale\bolletta-release.keystore `
  -p:AndroidSigningKeyAlias=bolletta `
  -p:AndroidSigningStorePass=LA_TUA_PASSWORD `
  -p:AndroidSigningKeyPass=LA_TUA_PASSWORD
```

Output: **`bin\Release\net9.0-android\publish\net.talete.bollettaanalyzer-Signed.aab`**
— è il file da caricare su Play Console.

Note:
- Il build esegue da solo `npm run build` del client React (con `.env.production`).
- `ApplicationId` è `net.talete.bollettaanalyzer`: **non potrà più cambiare** dopo il
  primo caricamento. Se vuoi cambiarlo (es. `it.talete.bollettaanalyzer`), fallo ORA
  nel csproj.
- A ogni upload successivo **incrementa `<ApplicationVersion>`** nel csproj (1 → 2 → 3…).

## 7️⃣ Google Play Console (1–2 h)

### Account
1. [play.google.com/console](https://play.google.com/console/signup) → registrati
   ($25). Se hai una società, valuta l'account **organizzazione**: niente vincolo
   dei 12 tester/14 giorni per la produzione.
2. Completa la verifica identità appena richiesta (è il collo di bottiglia).

### Crea l'app
**Crea app** → nome `Bolletta Analyzer`, lingua `Italiano`, tipo **App**, **Gratuita**.

### Compila le sezioni obbligatorie (dashboard → "Configura la tua app")
| Sezione | Cosa dichiarare |
|---|---|
| **Privacy policy** | URL pubblico (vedi passo 8) |
| **Accesso alle app** | l'app richiede login → fornisci a Google un **account di test** (creane uno dall'app: es. `revisore@test.it` / password robusta) |
| **Annunci** | No |
| **Classificazione contenuti** | questionario → categoria "Utility" → nessun contenuto sensibile |
| **Pubblico di destinazione** | 18+ (dati finanziari) |
| **Sicurezza dei dati** | vedi tabella sotto |
| **Categoria** | Strumenti (o Finanza) |

### Sezione "Sicurezza dei dati" — cosa dichiarare onestamente
| Dato | Raccolto? | Condiviso? | Motivo |
|---|---|---|---|
| Nome, email | ✅ | ❌ | funzionalità dell'app (account) |
| Indirizzo, telefono | ✅ (facoltativi) | ❌ | profilo utente |
| Info finanziarie (bollette) | ✅ | ❌ | funzionalità dell'app |
| Dati **cifrati in transito** | ✅ (HTTPS) | | |
| Cancellazione dati | ✅ l'utente può eliminare l'account dall'API (`DELETE /auth/account`) | | |

### Materiale grafico richiesto
- **Icona** 512×512 PNG → esporta da `Resources/AppIcon/appicon.svg` (+ fulmine)
- **Feature graphic** 1024×500 PNG → sfondo gradiente brand + logo + claim
- **Screenshot** (min 2, consigliati 4-6): dall'emulatore con l'app collegata al
  backend pubblico — Login, Dashboard, Simulatore, Profilo
  (`Ctrl+S` nell'emulatore salva lo screenshot)

### Caricamento
**Test interno** (Testing → Internal testing) → **Create release** → carica l'AAB →
aggiungi la tua email come tester → **Rollout**. Dopo pochi minuti hai il **link
d'installazione** da aprire sul telefono. 🎉

Poi (per sbloccare la produzione con account personale): **Test chiuso** → invita
almeno 12 tester → devono restare attivi 14 giorni → richiedi accesso a produzione.

## 8️⃣ Privacy policy (30 min)

Obbligatoria (l'app raccoglie email e dati finanziari). La via più rapida:

1. Genera il testo con [privacypolicies.com](https://www.privacypolicies.com) o
   simili (gratis per uso base), oppure scrivila su misura: titolare (tu), dati
   raccolti (email, nome, anagrafica facoltativa, dati bollette), scopo (analisi
   consumi personali), conservazione (fino a cancellazione account), diritti GDPR
   (accesso/export/cancellazione — l'API li supporta già), contatto.
2. Pubblicala come pagina web. Gratis in 5 minuti con **GitHub Pages**: crea un
   repo pubblico `privacy` con un `index.md`, attiva Pages → URL tipo
   `https://ivan-gismondi98.github.io/privacy/`.
3. Incolla l'URL in Play Console.

---

## 🔥 Domanda Firebase: risposta onesta

**Posso usare Firebase come database? Tecnicamente sì, praticamente no — e te lo sconsiglio per domani.**

Il motivo è architetturale: il tuo backend .NET usa **Entity Framework Core**, che
parla con database **relazionali** (SQLite/PostgreSQL — già supportati e configurabili
senza toccare il codice). Firestore/Realtime DB sono documentali e **non hanno
provider EF Core**: adottarli significherebbe riscrivere l'intero strato dati, l'
autenticazione (da JWT+BCrypt a Firebase Auth) e probabilmente spostare la logica in
Cloud Functions. Sono **giorni di lavoro**, buttando via il backend .NET che oggi
funziona — l'opposto di quello che ti serve entro domani.

**La scelta giusta con la tua architettura**: PostgreSQL gestito (Neon/Supabase,
passo 1) — è esattamente il ruolo che immagini per Firebase, ma compatibile al 100%
col codice esistente. Zero righe di codice da cambiare.

**Dove Firebase invece HA senso (dopo il lancio):** ti basterà creare il progetto
Firebase e aggiungere l'app Android:
- **Crashlytics** → crash report in produzione (utilissimo)
- **Cloud Messaging (FCM)** → le notifiche push della roadmap ("è ora dell'auto-lettura")
- **Analytics** → uso delle schermate

Se vorrai una di queste, dal progetto Firebase mi servirà solo il file
**`google-services.json`** (Impostazioni progetto → Le tue app → Android, package
`net.talete.bollettaanalyzer`) e integro io i pacchetti nel progetto MAUI.

---

## 📨 Cosa devi passarmi (e cosa NO)

**Passami:**
1. ✅ L'**URL pubblico del backend** una volta deployato (es.
   `https://bolletta-api.azurewebsites.net`) → aggiorno io `.env.production`,
   verifico la configurazione e preparo il resto
2. ✅ Quale hosting hai scelto (A o B), se qualcosa non torna
3. ✅ (solo se vorrai push/crash report) il `google-services.json`

**NON passarmi mai** (vanno solo nelle variabili d'ambiente dell'host):
- ❌ La connection string del database
- ❌ `Jwt__Key` / `Encryption__Key`
- ❌ Password del keystore

---

## ✅ Riepilogo finale: "app al 100%"

Quando hai completato i passi 1–6, l'app sui dispositivi reali:
- si installa dal link di test interno di Play
- registra utenti e fa login sull'API pubblica in HTTPS
- carica bollette PDF, le analizza (OCR reale), mostra dashboard e consigli
- salva contratti con PDF cifrato, auto-letture, simulazioni e previsioni
- gestisce export e cancellazione account (GDPR)

Restano **fuori** dalla v1 (roadmap in `falle.md`): OCR delle foto lato server
(attivabile col provider Azure), notifiche push, recupero password via email,
refresh token. Nessuno di questi blocca la pubblicazione.

Buon lancio! ⚡
