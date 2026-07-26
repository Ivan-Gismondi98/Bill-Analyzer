using BollettaAnalyzer.Domain.Common;
using BollettaAnalyzer.Domain.Enums;

namespace BollettaAnalyzer.Domain.Entities;

/// <summary>
/// Elettrodomestico/dispositivo censito dall'utente per il simulatore consumi.
/// </summary>
public class DispositivoElettronico : BaseEntity
{
    public Guid UtenteId { get; set; }
    public Utente? Utente { get; set; }

    public string Nome { get; set; } = string.Empty;

    /// <summary>Potenza nominale in Watt.</summary>
    public int PotenzaWatt { get; set; }

    /// <summary>Ore di utilizzo medie giornaliere.</summary>
    public decimal OreUtilizzoGiornaliere { get; set; }

    /// <summary>Giorni di utilizzo alla settimana (1-7).</summary>
    public int GiorniSettimana { get; set; } = 7;

    // --- Fasce orarie di utilizzo ---
    // Un dispositivo può essere usato in più fasce (es. il frigorifero è sempre
    // attivo: F1+F2+F3). Il costo viene ponderato sulle ore settimanali ARERA
    // di ciascuna fascia selezionata (vedi SimulazioneService).
    public bool UsaF1 { get; set; } = true;
    public bool UsaF2 { get; set; } = true;
    public bool UsaF3 { get; set; } = true;

    /// <summary>Fasce di utilizzo selezionate, in forma di lista.</summary>
    public IReadOnlyList<FasciaOraria> Fasce
    {
        get
        {
            var fasce = new List<FasciaOraria>(3);
            if (UsaF1) fasce.Add(FasciaOraria.F1);
            if (UsaF2) fasce.Add(FasciaOraria.F2);
            if (UsaF3) fasce.Add(FasciaOraria.F3);
            return fasce;
        }
    }

    /// <summary>kWh consumati in un giorno = (Watt * ore) / 1000.</summary>
    public decimal ConsumoGiornalieroKwh => Math.Round(PotenzaWatt * OreUtilizzoGiornaliere / 1000m, 3);

    /// <summary>Stima consumo mensile in kWh.</summary>
    public decimal ConsumoMensileKwh => Math.Round(ConsumoGiornalieroKwh * (GiorniSettimana / 7m) * 30m, 2);
}
