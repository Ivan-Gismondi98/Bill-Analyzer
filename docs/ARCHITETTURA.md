# Architettura — Bolletta Analyzer

## Clean Architecture (backend)

```
Api  ──►  Application  ──►  Domain
 │             ▲
 └──►  Infrastructure ──────┘
```

- **Domain** — entità pure e enum, nessuna dipendenza esterna.
  `Utente`, `Contratto`, `Bolletta` (+ `VoceDiCosto`), `LetturaContatore`, `DispositivoElettronico`.
- **Application** — contratti applicativi:
  - DTO di input/output
  - interfacce (`IBillOcrService`, `IJwtTokenGenerator`, `IPasswordHasher`, `ICurrentUser`,
    `ISuggerimentiService`, `ISimulazioneService`)
  - logica di dominio pura: `SuggerimentiService` (motore consigli) e `SimulazioneService`
    (calcolo consumi e previsione).
- **Infrastructure** — dettagli tecnici:
  - `AppDbContext` + configurazioni EF Core, provider SQLite/PostgreSQL
  - Auth: `JwtTokenGenerator`, `PasswordHasher` (BCrypt), `CurrentUser`
  - `MockBillOcrService` (sostituibile)
  - `DbSeeder` con dati demo.
- **Api** — controller REST sottili che orchestrano Application/Infrastructure, autenticazione
  JWT, CORS, Swagger, migrazione+seed all'avvio.

## Formule di dominio

- **Consumo dispositivo**: `kWh_giorno = (Watt × ore_giorno) / 1000`
  `kWh_mese = kWh_giorno × (giorni_settimana / 7) × 30`
- **Previsione bolletta**: dal delta di due auto-letture si ricava la media giornaliera,
  proiettata su un periodo bimestrale standard (60 gg); si applicano prezzo materia prima,
  quota fissa, accise e IVA.
- **Ripartizione fasce**: `%Fx = consumoFx / (F1+F2+F3) × 100`.
- **Suggerimenti**: euristiche su prezzo materia prima vs benchmark di mercato, dominanza F1,
  potenza impegnata sovradimensionata, prezzo gas.

## Frontend Hybrid

Il progetto MAUI ospita un `HybridWebView` che carica la SPA React buildata
(`Resources/Raw/wwwroot`). Vantaggi:
- **una sola UI** condivisa tra Android, iOS, macOS, Windows e web;
- sviluppo rapido con Vite/hot-reload lato React;
- accesso alle capacità native tramite l'interop JS ↔ C# dell'HybridWebView
  (es. fotocamera, file system) quando servirà.

Routing con **React Router** (`HashRouter`, robusto per il caricamento da package locale).
Stato di autenticazione in `AuthContext`; token JWT persistito in `localStorage` e iniettato
via interceptor Axios.

## Estensioni previste
- OCR reale (Azure Document Intelligence / Tesseract) dietro `IBillOcrService`.
- Persistenza offline lato client (SQLite locale) e sincronizzazione.
- Interop nativo HybridWebView per acquisizione foto bolletta.
