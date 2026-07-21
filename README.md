# Bolletta Analyzer

App **mobile e desktop** (.NET MAUI + React Hybrid) con backend **ASP.NET Core Web API**
per **comprendere le bollette di luce e gas**, analizzare i consumi, ricevere consigli di
risparmio ed effettuare **stime e simulazioni** di spesa futura.

> Sotto-progetto della soluzione SmartERP, completamente autonomo nella cartella `bolletta-analyzer/`.

---

## 🏛️ Architettura

```
bolletta-analyzer/
├─ BollettaAnalyzer.sln
├─ src/
│  ├─ Backend/                         # ASP.NET Core Web API — Clean Architecture
│  │  ├─ BollettaAnalyzer.Domain/          # Entità di dominio + enum (nessuna dipendenza)
│  │  ├─ BollettaAnalyzer.Application/     # DTO, interfacce, servizi di dominio (suggerimenti, simulazione)
│  │  ├─ BollettaAnalyzer.Infrastructure/  # EF Core (SQLite/PostgreSQL), Auth JWT, OCR reale, seed
│  │  └─ BollettaAnalyzer.Api/             # Controller REST, Program.cs, Swagger, CORS
│  └─ Frontend/
│     └─ BollettaAnalyzer.Maui/         # Host MAUI (HybridWebView) per Android/iOS/macOS/Windows
│        └─ ClientApp/                     # SPA React + Vite + TypeScript + Tailwind + Recharts
└─ docs/                                # Documentazione architetturale
```

| Livello   | Tecnologia |
|-----------|------------|
| Frontend  | .NET MAUI (HybridWebView, .NET 9) + React 18 + Vite + TypeScript + Tailwind + Recharts |
| Backend   | ASP.NET Core Web API .NET 9 — Clean Architecture (Domain/Application/Infrastructure/Api) |
| Database  | SQLite (default, offline) o PostgreSQL/SQL Server via EF Core (switch da configurazione) |
| Auth      | JWT Bearer + hashing password BCrypt |
| OCR       | `IBillOcrService` reale: PdfPig + Tesseract (`Local`) o Azure Document Intelligence (`Azure`), con parser bollette ARERA |

---

## 📱 Schermate & flusso

| Vista | Descrizione | Rotta React |
|-------|-------------|-------------|
| **Landing** | Hero + onboarding a carosello + CTA Accedi/Registrati | `/` |
| **Auth** | Login / registrazione con gestione token JWT | `/auth` |
| **Profilo & Contratti** | Anagrafica utente + configurazione contratti Luce/Gas (tariffa, €/kWh F1-F2-F3, €/Sm³, potenza kW) | `/profilo` |
| **Dashboard & Analisi** | Upload bolletta (PDF/foto), breakdown spesa, grafici fasce F1/F2/F3, suggerimenti | `/dashboard` |
| **Simulatore & Previsione** | Lista elettrodomestici, calcolo impatto (kWh = W×h/1000), auto-lettura contatore, previsione prossima bolletta | `/simulatore` |

---

## 🔌 Endpoint REST principali

| Metodo | Rotta | Descrizione |
|--------|-------|-------------|
| POST | `/api/auth/register` | Registrazione utente → JWT |
| POST | `/api/auth/login` | Login → JWT |
| GET  | `/api/auth/me` | Profilo utente corrente |
| PUT  | `/api/auth/profilo` | Aggiorna anagrafica |
| GET/POST/PUT/DELETE | `/api/contratti` | CRUD contratti Luce/Gas |
| GET/POST/DELETE | `/api/contratti/{id}/documento` | PDF contratto **cifrato a riposo** (metadati / upload / elimina) |
| GET  | `/api/contratti/{id}/documento/download` | Scarica il PDF (decifrato al volo) |
| GET  | `/api/bollette` | Elenco bollette (solo l'ultima per contratto) |
| GET  | `/api/bollette/{id}` | Analisi completa (breakdown + fasce + suggerimenti) |
| POST | `/api/bollette/upload` | Upload PDF/immagine → OCR → analisi (**sostituisce** la precedente) |
| GET/POST | `/api/letture` | Auto-letture contatore (`/ultima` per l'ultima; storico max 3) |
| GET/POST/PUT/DELETE | `/api/dispositivi` | CRUD elettrodomestici |
| POST | `/api/simulazione/dispositivi` | Impatto aggregato consumi elettrodomestici |
| POST | `/api/simulazione/previsione` | Previsione prossima bolletta da auto-lettura |

Swagger disponibile in sviluppo su `http://localhost:5080/swagger`.

---

## ▶️ Come avviare

### Prerequisiti
- .NET SDK **9.0** (`dotnet --version`)
- Workload MAUI: `dotnet workload install maui`
- Node.js **20+** e npm

### 1) Backend API
```bash
cd src/Backend/BollettaAnalyzer.Api
dotnet run
# API su http://localhost:5080  ·  Swagger su /swagger
# Al primo avvio crea il DB SQLite e inserisce dati demo.
```
Utente demo: **demo@bolletta.app** / **Password1!**

### 2) Client React (sviluppo web, hot reload)
```bash
cd src/Frontend/BollettaAnalyzer.Maui/ClientApp
npm install
cp .env.example .env.local     # imposta VITE_USE_MOCK=false per usare l'API reale
npm run dev                    # http://localhost:5173 (proxy /api -> :5080)
```
> Con `VITE_USE_MOCK=true` la SPA funziona **senza backend**, usando i dati mock.

### 3) App MAUI (mobile/desktop)
```bash
cd src/Frontend/BollettaAnalyzer.Maui
dotnet build -t:Run -f net9.0-android      # oppure net9.0-windows... / net9.0-ios / net9.0-maccatalyst
```
La build MAUI compila automaticamente il client React (target `BuildReactClient`) e ne
impacchetta l'output in `Resources/Raw/wwwroot`, servito dall'`HybridWebView`.
Disattivabile con `-p:SkipClientBuild=true`.

---

## 🗄️ Database: SQLite ↔ PostgreSQL
In `appsettings.json`:
```json
"Database": { "Provider": "Sqlite" },      // oppure "Postgres"
"ConnectionStrings": { "Default": "Data Source=bolletta.db" }
```

## 🔒 Sicurezza & conservazione dati
- **Contratto (PDF)**: conservato **cifrato a riposo** con AES-256-GCM
  (`IFileEncryptionService`). Nel database non transita mai in chiaro; la chiave è
  in `Encryption:Key` (Base64 32 byte), da fornire via secret/variabile d'ambiente
  in produzione.
- **Bollette**: dell'upload si tiene **solo il dato estratto dell'ultima** bolletta
  per contratto (sostituita al prossimo upload). Il file originale **non** viene
  salvato: server e memoria restano leggeri, i dati precisi.
- **Auto-letture**: storico a scorrimento delle **ultime 3** letture per contratto;
  le più vecchie vengono rimosse automaticamente.

## 🧾 OCR bollette (reale)
L'analisi delle bollette è implementata con una pipeline **a provider selezionabili**
tramite la sezione `Ocr` di `appsettings.json` (`Ocr:Provider`):

| Provider | Come funziona | Quando usarlo |
|----------|---------------|---------------|
| **`Local`** (default) | PDF digitali → estrazione testo con **PdfPig**; immagini/foto → OCR reale con **Tesseract**; il testo viene poi analizzato da `ItalianBillParser` | Nessun servizio cloud; i PDF elettronici funzionano out-of-the-box |
| **`Azure`** | **Azure AI Document Intelligence** (`prebuilt-invoice`) per OCR robusto anche su scansioni, arricchito dal parser di dominio | Scansioni/foto complesse, massima accuratezza |
| **`Mock`** | dati fittizi, nessuna analisi | Demo/sviluppo senza dipendenze |

Il **parser italiano** (`ItalianBillParser`) è indipendente dalla sorgente OCR ed estrae:
importo totale, numero fattura, periodo di competenza, consumi per fascia **F1/F2/F3**,
consumo gas in **Smc** e le **voci di costo ARERA** (materia energia, trasporto e gestione,
oneri di sistema, imposte/IVA), con una **confidenza** calcolata sui campi trovati.

Configurazione:

```jsonc
"Ocr": {
  "Provider": "Local",            // Local | Azure | Mock
  "MinPdfTextLength": 40,
  "Tesseract": { "DataPath": "./tessdata", "Language": "ita" },
  "Azure": { "Endpoint": "", "ApiKey": "", "ModelId": "prebuilt-invoice" }
}
```

- **Local + immagini**: serve la cartella `tessdata` con `ita.traineddata`
  ([download langdata](https://github.com/tesseract-ocr/tessdata)). Su Linux installa anche
  le librerie native (`apt install libtesseract-dev libleptonica-dev`).
- **Azure**: imposta `Endpoint` e `ApiKey` (preferibilmente via secret/variabile d'ambiente).
- I documenti illeggibili restituiscono **422** con un messaggio chiaro (non un 500).

Vedi [`docs/ARCHITETTURA.md`](docs/ARCHITETTURA.md) per i dettagli.
