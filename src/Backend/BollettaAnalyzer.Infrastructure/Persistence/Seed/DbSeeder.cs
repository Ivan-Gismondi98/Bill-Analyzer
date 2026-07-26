using BollettaAnalyzer.Application.Common.Interfaces;
using BollettaAnalyzer.Domain.Entities;
using BollettaAnalyzer.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BollettaAnalyzer.Infrastructure.Persistence.Seed;

/// <summary>
/// Popola il database con dati demo pronti per il testing:
/// un utente (demo@bolletta.app / Password1!), un contratto luce, uno gas,
/// una bolletta di esempio con breakdown e alcuni elettrodomestici.
/// </summary>
public static class DbSeeder
{
    public const string DemoEmail = "demo@bolletta.app";
    public const string DemoPassword = "Password1!";

    public static async Task SeedAsync(AppDbContext db, IPasswordHasher hasher, CancellationToken ct = default)
    {
        if (await db.Utenti.AnyAsync(ct)) return;

        var utente = new Utente
        {
            Email = DemoEmail,
            PasswordHash = hasher.Hash(DemoPassword),
            Nome = "Mario",
            Cognome = "Rossi",
            Telefono = "+39 333 1234567",
            Indirizzo = "Via Roma 1",
            Citta = "Milano",
            Cap = "20100"
        };

        var contrattoLuce = new Contratto
        {
            Utente = utente,
            TipoFornitura = TipoFornitura.Luce,
            Fornitore = "Enel Energia",
            CodicePod = "IT001E12345678",
            NomeOfferta = "Luce Web Trioraria",
            TipoTariffa = TipoTariffa.Multioraria,
            PotenzaImpegnataKw = 3.0m,
            PrezzoKwhMonorario = 0.145m,
            PrezzoKwhF1 = 0.170m,
            PrezzoKwhF2 = 0.150m,
            PrezzoKwhF3 = 0.120m,
            QuotaFissaMensile = 9.50m,
            DataAttivazione = new DateTime(2024, 1, 15)
        };

        var contrattoGas = new Contratto
        {
            Utente = utente,
            TipoFornitura = TipoFornitura.Gas,
            Fornitore = "Eni Plenitude",
            CodicePdr = "01234567890123",
            NomeOfferta = "Gas Trend Casa",
            TipoTariffa = TipoTariffa.Monoraria,
            PrezzoSm3 = 0.520m,
            QuotaFissaMensile = 8.00m,
            DataAttivazione = new DateTime(2024, 1, 15)
        };

        var bolletta = new Bolletta
        {
            Contratto = contrattoLuce,
            NumeroFattura = "2026/000123",
            PeriodoInizio = new DateTime(2026, 3, 1),
            PeriodoFine = new DateTime(2026, 4, 30),
            DataEmissione = new DateTime(2026, 5, 8),
            ConsumoTotaleKwh = 412m,
            ConsumoF1Kwh = 198m,
            ConsumoF2Kwh = 92m,
            ConsumoF3Kwh = 122m,
            ImportoTotale = 98.74m,
            DaOcr = false,
            VociDiCosto = new List<VoceDiCosto>
            {
                new() { Categoria = CategoriaCosto.MateriaEnergia, Descrizione = "Spesa per la materia energia", Importo = 58.20m },
                new() { Categoria = CategoriaCosto.TrasportoGestione, Descrizione = "Trasporto e gestione contatore", Importo = 17.30m },
                new() { Categoria = CategoriaCosto.OneriDiSistema, Descrizione = "Oneri di sistema", Importo = 12.40m },
                new() { Categoria = CategoriaCosto.Imposte, Descrizione = "Imposte e IVA", Importo = 10.84m },
            }
        };

        // Storico auto-letture (le ultime 3 vengono mantenute lato applicazione).
        var letture = new List<LetturaContatore>
        {
            new()
            {
                Contratto = contrattoLuce,
                DataLettura = new DateTime(2026, 4, 30),
                ValoreTotale = 15420m,
                ValoreF1 = 7100m, ValoreF2 = 4200m, ValoreF3 = 4120m,
                DaBolletta = true,
                Note = "Lettura di chiusura ultima bolletta"
            },
            new()
            {
                Contratto = contrattoLuce,
                DataLettura = new DateTime(2026, 5, 31),
                ValoreTotale = 15588m,
                Note = "Auto-lettura mensile"
            },
            new()
            {
                Contratto = contrattoLuce,
                DataLettura = new DateTime(2026, 6, 20),
                ValoreTotale = 15680m
            },
        };

        var dispositivi = new List<DispositivoElettronico>
        {
            // Frigorifero: sempre attivo, tutte le fasce.
            new() { Utente = utente, Nome = "Frigorifero", PotenzaWatt = 150, OreUtilizzoGiornaliere = 24m, GiorniSettimana = 7, UsaF1 = true, UsaF2 = true, UsaF3 = true },
            new() { Utente = utente, Nome = "Lavatrice", PotenzaWatt = 2000, OreUtilizzoGiornaliere = 1.5m, GiorniSettimana = 4, UsaF1 = true, UsaF2 = false, UsaF3 = false },
            new() { Utente = utente, Nome = "Forno elettrico", PotenzaWatt = 2200, OreUtilizzoGiornaliere = 0.5m, GiorniSettimana = 5, UsaF1 = false, UsaF2 = true, UsaF3 = false },
            new() { Utente = utente, Nome = "Condizionatore", PotenzaWatt = 1200, OreUtilizzoGiornaliere = 4m, GiorniSettimana = 6, UsaF1 = true, UsaF2 = true, UsaF3 = false },
        };

        db.Utenti.Add(utente);
        db.Contratti.AddRange(contrattoLuce, contrattoGas);
        db.Bollette.Add(bolletta);
        db.Letture.AddRange(letture);
        db.Dispositivi.AddRange(dispositivi);

        await db.SaveChangesAsync(ct);
    }
}
