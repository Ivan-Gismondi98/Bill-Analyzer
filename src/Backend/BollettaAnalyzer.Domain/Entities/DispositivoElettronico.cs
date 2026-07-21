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

    /// <summary>Fascia oraria prevalente di utilizzo.</summary>
    public FasciaOraria FasciaPrevalente { get; set; } = FasciaOraria.F1;

    /// <summary>kWh consumati in un giorno = (Watt * ore) / 1000.</summary>
    public decimal ConsumoGiornalieroKwh => Math.Round(PotenzaWatt * OreUtilizzoGiornaliere / 1000m, 3);

    /// <summary>Stima consumo mensile in kWh.</summary>
    public decimal ConsumoMensileKwh => Math.Round(ConsumoGiornalieroKwh * (GiorniSettimana / 7m) * 30m, 2);
}
