# 🔍 Audit di sicurezza e logica — Bolletta Analyzer

> Revisione completa del codice (backend .NET, OCR, auth, cifratura, EF Core, frontend React).
> Le falle sono elencate **dalla più grave alla meno grave**. Per ciascuna: dove si trova,
> perché è un problema, e come correggerla.
>
> Nota positiva: tutti gli endpoint verificano correttamente la proprietà delle risorse
> (`UtenteId` sul contratto/bolletta/lettura/dispositivo) — **non ho trovato IDOR**.

---

## 🔴 CRITICHE

### 1. Chiave JWT di fallback hardcoded nel codice sorgente
**Dove:** `src/Backend/BollettaAnalyzer.Api/Program.cs:26` e `src/Backend/BollettaAnalyzer.Infrastructure/DependencyInjection.cs:32`

```csharp
if (string.IsNullOrWhiteSpace(jwt.Key))
    jwt.Key = "CHANGE_ME_super_secret_dev_key_min_32_chars_length!!";
```

Se l'app viene deployata senza impostare `Jwt:Key` (l'`appsettings.json` di default la lascia
vuota!), i token vengono firmati con una chiave **pubblica su GitHub**. Chiunque può forgiarsi
un JWT valido con l'ID di qualunque utente e accedere a tutti i dati: **bypass totale
dell'autenticazione**. Peggio: il fallback silenzioso fa sembrare tutto funzionante.

**Fix:** in produzione **rifiutarsi di partire** se la chiave manca o è quella di default:
```csharp
if (string.IsNullOrWhiteSpace(jwt.Key))
{
    if (!builder.Environment.IsDevelopment())
        throw new InvalidOperationException("Jwt:Key non configurata: impostarla via variabile d'ambiente/secret.");
    jwt.Key = "...solo dev...";
}
```

### 2. Utente demo con credenziali pubbliche creato anche in produzione
**Dove:** `src/Backend/BollettaAnalyzer.Api/Program.cs:81-87` + `DbSeeder.cs:15-16`

Il commento dice "seed automatici in sviluppo", ma il blocco **gira sempre**, in qualsiasi
ambiente. Al primo avvio in produzione (DB vuoto) viene creato `demo@bolletta.app /
Password1!` — credenziali scritte nel repository. Chiunque le legge entra in produzione.

**Fix:** condizionare il seed a `app.Environment.IsDevelopment()` (o a un flag
`Seed:Enabled=false` di default) e generare una password casuale loggata solo in dev.

---

## 🟠 ALTE

### 3. Chiave di cifratura documenti con fallback hardcoded
**Dove:** `src/Backend/BollettaAnalyzer.Infrastructure/Services/AesFileEncryptionService.cs:67`

```csharp
return SHA256.HashData("dev-only-encryption-key-change-me"u8.ToArray());
```

Se `Encryption:Key` non è configurata (default: vuota), i PDF dei contratti — dati personali
sensibili — vengono cifrati con una chiave derivata da una stringa **pubblica**. La cifratura
diventa solo apparente. Inoltre non c'è gestione della rotazione: cambiare chiave rende
illeggibili i documenti esistenti e il download va in **500** (la `CryptographicException`
di AES-GCM non è gestita da nessun handler).

**Fix:** stesso pattern del punto 1 (fail-fast in produzione) + try/catch sul `Decrypt` con
messaggio chiaro (409/422 "documento non decifrabile, ricaricarlo") + prevedere key-versioning
(salvare l'ID versione della chiave accanto al nonce).

### 4. BUG LOGICO: la previsione bolletta calcola sempre consumo ≈ 0 dall'app
**Dove:** `SimulatorePage.tsx:161-175` (frontend) + `SimulazioneController.cs:50-55` (backend)

Il flusso UI fa: **prima** salva l'auto-lettura (`lettureApi.crea`), **poi** chiama la
previsione. Ma il backend prende come base "l'ultima lettura del contratto"… che ora è
**quella appena salvata, con lo stesso valore e la stessa data** della richiesta:

```
consumoPeriodo = Max(0, LetturaAttuale - ultimaLettura.ValoreTotale)  // = 0 !
giorni = Max(1, (dataAttuale - dataUltima).Days)                       // = 1
```

Risultato: consumo di periodo **0**, previsione = solo quota fissa + IVA. La funzione
principale del simulatore produce sempre un valore fasullo (es. €23 invece di €96).

**Fix (una delle due):**
- backend: nel calcolo della previsione escludere le letture con stesso valore/timestamp
  della richiesta, ossia prendere l'ultima lettura **precedente** a `DataLetturaAttuale`
  (`l.DataLettura < req.DataLetturaAttuale` e in pratica la penultima); oppure
- frontend: chiamare **prima** la previsione e **poi** salvare la lettura.

### 5. CORS: qualsiasi origin con credenziali abilitate
**Dove:** `src/Backend/BollettaAnalyzer.Api/Program.cs:47-51`

```csharp
p.AllowAnyHeader().AllowAnyMethod()
 .SetIsOriginAllowed(_ => true)   // qualsiasi sito
 .AllowCredentials();
```

`SetIsOriginAllowed(_ => true)` è il workaround che aggira il divieto di ASP.NET di combinare
`AllowAnyOrigin` con `AllowCredentials`. Qualsiasi sito web può fare richieste credenziali
verso l'API. Oggi il rischio pratico è mitigato dal Bearer token (non cookie), ma se un domani
si passasse a cookie/sessione diventerebbe CSRF totale — ed è comunque una configurazione da
non portare in produzione.

**Fix:** allowlist esplicita di origin da configurazione (`Cors:AllowedOrigins`), col
wildcard solo in Development per l'HybridWebView.

---

## 🟡 MEDIE

### 6. Nessun rate limiting né lockout su login/register (brute force + enumerazione utenti)
**Dove:** `AuthController.cs` (tutto), `Program.cs`

- `POST /auth/login` è attaccabile a dizionario senza alcun limite.
- `POST /auth/register` risponde `409 "Email già registrata"` → un attaccante può
  **enumerare le email registrate**.
- Anche il login ha un oracolo temporale: se l'utente non esiste, BCrypt non viene eseguito
  (risposta più veloce).

**Fix:** middleware `AddRateLimiter` di ASP.NET (fixed-window per IP su `/auth/*`), lockout
progressivo per account, risposta di registrazione neutra ("Se l'email è disponibile riceverai
conferma") e verifica di un hash dummy quando l'utente non esiste.

### 7. L'upload bolletta cancella i dati precedenti anche se l'OCR è inaffidabile
**Dove:** `BolletteController.cs:79-101` + `ItalianBillParser.cs`

Le bollette precedenti vengono rimosse e sostituite **prima** di qualsiasi controllo di
qualità sul risultato OCR. Il parser, quando non trova nulla, può restituire importo 0,
consumi 0 e — peggio — un **periodo inventato** (fallback: "ultimi 2 mesi da oggi") che viene
salvato come dato reale. La `ConfidenzaMedia` calcolata dal parser **non viene né salvata né
mostrata** all'utente.

**Fix:** rifiutare (422) risultati con confidenza sotto soglia (es. 0.35) o con
`ImportoTotale <= 0`; persistere `ConfidenzaMedia` sulla `Bolletta` e mostrarla in UI
("dati estratti con confidenza 62% — verifica i valori"); permettere modifica manuale.

### 8. Nessuna validazione di dominio sugli input numerici e sulle date
**Dove:** `LetturaDtos.cs`, `ContrattoDtos.cs`, `DispositivoDtos.cs`, relativi controller

Accettati senza errori: letture **negative**, letture con **data futura**, lettura totale
inferiore alla precedente (contatore che "torna indietro"), prezzi kWh negativi, potenza
negativa, `GiorniSettimana = 900`, ore giornaliere = 50. Tutti questi finiscono dritti nei
calcoli di previsione/simulazione producendo risultati assurdi (il `Math.Max(0, …)` nel
servizio maschera il problema invece di segnalarlo).

**Fix:** DataAnnotations sui DTO (`[Range(0, …)]`, `[Range(1,7)]`, `[Range(0,24)]`) +
regole di dominio nel controller: data lettura non futura, valore ≥ ultima lettura
(o richiesta esplicita di conferma "contatore sostituito").

### 9. Upload documento contratto: nessuna validazione del tipo di file
**Dove:** `ContrattiController.cs:94-118`

Si può caricare **qualsiasi file** (eseguibile, HTML, script): né l'estensione, né i magic
byte, né il `ContentType` (che è controllato dal client e salvato tal quale) vengono
verificati. Il file viene poi riservito col `ContentType` fornito dall'attaccante. Il
`Content-Disposition: attachment` mitiga l'esecuzione nel browser, ma il canale resta un
deposito di file arbitrari cifrati.

**Fix:** accettare solo `%PDF` (magic byte) + forzare `ContentType = application/pdf` +
sanificare `NomeFile` (via `Path.GetFileName`).

### 10. `EnsureCreatedAsync()` al posto delle migrazioni
**Dove:** `Program.cs:84`

`EnsureCreated` crea lo schema una tantum e **rende impossibili le migrazioni future**: alla
prima modifica delle entità, i DB esistenti non verranno mai aggiornati (errori a runtime su
colonne mancanti) e `Database.Migrate()` non funzionerà su DB creati così.

**Fix:** generare le migrazioni EF (`dotnet ef migrations add Initial`) e usare
`db.Database.MigrateAsync()`.

### 11. Nessun handler globale delle eccezioni
**Dove:** `Program.cs` (assente)

Fuori dall'unico try/catch dell'upload bolletta, ogni eccezione (decifratura fallita, DB
corrotto, bug) produce un **500 grezzo**; in Development la developer exception page espone
stack trace e query. Manca anche il logging strutturato degli errori.

**Fix:** `app.UseExceptionHandler()` con `ProblemDetails` + mappatura di
`OcrParsingException`/`CryptographicException` su status code sensati.

### 12. Manca cancellazione account ed export dati (GDPR)
**Dove:** API (assente)

L'app conserva dati personali (anagrafica, telefono, indirizzo) e documenti contrattuali,
ma non esiste alcun endpoint per **eliminare l'account** (il cascade su DB c'è già:
`UtenteConfig`) né per **esportare i propri dati**. Per un'app destinata al mercato è un
requisito normativo, non un nice-to-have.

**Fix:** `DELETE /api/auth/account` (con ri-autenticazione) + `GET /api/auth/export` (JSON).

---

## 🟢 BASSE

### 13. Token JWT in `localStorage`
**Dove:** `ClientApp/src/api/client.ts:14-18` — vulnerabile a esfiltrazione via XSS (nessuna
XSS nota oggi, ma è il design a rischio). Dentro l'HybridWebView MAUI valutare il secure
storage nativo via interop; sul web valutare cookie `HttpOnly` + anti-CSRF.

### 14. Password policy debole
**Dove:** `AuthDtos.cs:7` — solo `MinLength(6)`. Nessun controllo di complessità o contro
password comuni; la stessa demo è `Password1!`. Portare a 8+ con blacklist (es. top-10k).

### 15. Niente HTTPS redirect / HSTS / security headers
**Dove:** `Program.cs` — mancano `UseHttpsRedirection()`, HSTS e header di sicurezza
(`X-Content-Type-Options`, `Content-Security-Policy` per eventuali pagine servite).

### 16. Sessioni non revocabili e senza refresh
**Dove:** `AuthServices.cs` — JWT valido 8 ore senza possibilità di revoca (logout solo
client-side, il token resta valido). Nessun refresh token: alla scadenza l'utente viene
buttato fuori a metà operazione. Valutare refresh token con rotazione + denylist su logout.

### 17. `ManteniUltime3` cancella anche la lettura "da bolletta" e fa un doppio SaveChanges
**Dove:** `LettureController.cs:77-91` — la baseline `DaBolletta=true` (riferimento prezioso
per le previsioni) viene eliminata come le altre appena si superano 3 letture; i due
`SaveChangesAsync` separati non sono atomici. Escludere `DaBolletta` dal conteggio o salvare
in un'unica transazione.

### 18. `TesseractEngine` ricreato a ogni richiesta
**Dove:** `TesseractOcrEngine.cs:31` — l'engine è costoso da inizializzare (carica i
traineddata). Sotto carico diventa un collo di bottiglia. Usare un pool o un singleton
con lock.

### 19. Fallback rischioso del parser: "il kWh più grande nel testo"
**Dove:** `ItalianBillParser.cs` (`EstraiConsumoTotaleKwh`, ultima spiaggia) — può agganciare
numeri non pertinenti (es. consumo annuo indicato a fini informativi → sovrastima del
periodo). Limitare il fallback ai casi in cui la somma fasce è assente e marcare il dato a
bassa confidenza.

---
---

# 🚀 Cosa NON può mancare (e cosa renderebbe l'app unica sul mercato)

## Fondamenta irrinunciabili (prima del lancio)
1. **Correzione manuale post-OCR** — l'OCR sbaglierà sempre qualcosa: una schermata di
   revisione ("ecco cosa ho letto, correggi se serve") trasforma un difetto in fiducia.
2. **Storico bollette pluriennale con trend** — oggi si tiene solo l'ultima bolletta:
   senza serie storica non esistono grafici di andamento, confronti anno-su-anno, né la
   domanda chiave "sto pagando più dell'anno scorso a parità di consumo?".
3. **Recupero password + verifica email + 2FA/biometria** (Face ID/impronta via MAUI) —
   oggi manca perfino il reset password.
4. **Notifiche push intelligenti** — "è ora dell'auto-lettura", "bolletta prevista sopra
   €120", "la tua offerta scade tra 30 giorni". L'app diventa proattiva invece che consultiva.

## Differenziatori che la renderebbero unica in Italia 🇮🇹
5. **Confronto offerte in tempo reale con dati ufficiali ARERA** — incrociare il profilo di
   consumo reale dell'utente (fasce, stagionalità) con il **Portale Offerte ARERA** e i prezzi
   PUN/PSV correnti: non il solito comparatore generico, ma "con *i tuoi* kWh distribuiti
   *così*, l'offerta X ti farebbe risparmiare €137/anno". Nessuna app consumer italiana lo fa
   bene partendo dalla bolletta vera.
6. **Inoltro bollette via email** — un indirizzo personale (`mario.abc123@bollette.app`):
   l'utente gira l'email del fornitore e l'app importa da sola PDF/XML. Zero attrito: è il
   killer-feature dell'acquisizione dati.
7. **Assistente AI conversazionale sulla bolletta** — "perché questa bolletta è più alta?",
   "cos'è la voce oneri di sistema?", "conviene passare alla tariffa monoraria?" con risposte
   calcolate sui dati reali dell'utente (LLM + i dati strutturati già estratti). Trasforma
   l'app da lettore a consulente.
8. **Simulatore investimenti energetici** — fotovoltaico, pompa di calore, piano a induzione:
   payback calcolato sui consumi reali per fascia dell'utente (i dati ci sono già!), con
   detrazioni fiscali italiane correnti. Nessun competitor lo integra con i dati di bolletta.
9. **Benchmark anonimo di quartiere** — "consumi il 23% in più delle famiglie simili nella
   tua zona" (clustering su CAP + potenza + componenti). Il confronto sociale è il motore di
   risparmio più efficace documentato.
10. **Modalità famiglia / multi-immobile** — casa + seconda casa + casa dei genitori
    anziani in un'unica dashboard con inviti multi-utente. Le utility app sono tutte
    mono-utenza: qui c'è uno spazio libero.
11. **Punteggio "salute energetica" + CO₂** — un indice 0-100 che sintetizza prezzo pagato
    vs mercato, distribuzione fasce ed efficienza, con storia gamificata dei miglioramenti
    e l'equivalente in CO₂ risparmiata.
12. **Switch assistito** — chiusura del cerchio: dal suggerimento "risparmi €137 con X" al
    cambio fornitore guidato in-app (referral/partnership = anche il modello di business).

## Tocchi di modernità
- **Widget home-screen** (spesa mese corrente, countdown auto-lettura) e **watch app**.
- **Import foto contatore** con lettura automatica delle cifre (il modello OCR c'è già).
- **Dark mode** e accessibilità completa (oggi il tema è solo chiaro).
- **Offline-first** con sync (già previsto in roadmap architetturale).

> Il filo conduttore: gli altri comparatori partono da stime; questa app conosce i **consumi
> reali per fascia** dell'utente. Ogni funzionalità che sfrutta questo dato (confronto offerte,
> simulatore investimenti, benchmark) è per costruzione più precisa di qualsiasi concorrente.
