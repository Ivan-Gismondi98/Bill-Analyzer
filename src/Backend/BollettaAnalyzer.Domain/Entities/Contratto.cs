using BollettaAnalyzer.Domain.Common;
using BollettaAnalyzer.Domain.Enums;

namespace BollettaAnalyzer.Domain.Entities;

/// <summary>
/// Contratto di fornitura Luce o Gas configurato dall'utente.
/// I prezzi unitari sono usati per stime e simulazioni.
/// </summary>
public class Contratto : BaseEntity
{
    public Guid UtenteId { get; set; }
    public Utente? Utente { get; set; }

    public TipoFornitura TipoFornitura { get; set; }
    public string Fornitore { get; set; } = string.Empty;
    public string? CodicePod { get; set; }   // POD per luce (IT...)
    public string? CodicePdr { get; set; }   // PDR per gas
    public string NomeOfferta { get; set; } = string.Empty;

    // --- Parametri LUCE ---
    public TipoTariffa TipoTariffa { get; set; } = TipoTariffa.Monoraria;

    /// <summary>Potenza impegnata in kW (tipicamente 3.0).</summary>
    public decimal PotenzaImpegnataKw { get; set; }

    /// <summary>Prezzo €/kWh monorario (usato quando TipoTariffa = Monoraria).</summary>
    public decimal PrezzoKwhMonorario { get; set; }

    public decimal PrezzoKwhF1 { get; set; }
    public decimal PrezzoKwhF2 { get; set; }
    public decimal PrezzoKwhF3 { get; set; }

    // --- Parametri GAS ---
    /// <summary>Prezzo €/Sm3 per il gas.</summary>
    public decimal PrezzoSm3 { get; set; }

    /// <summary>Coefficiente di conversione Smc -> Sm3 (default 1.0).</summary>
    public decimal CoefficienteConversione { get; set; } = 1.0m;

    // --- Costi fissi comuni ---
    /// <summary>Quota fissa mensile di commercializzazione (€/mese).</summary>
    public decimal QuotaFissaMensile { get; set; }

    public DateTime DataAttivazione { get; set; } = DateTime.UtcNow;
    public bool Attivo { get; set; } = true;

    // Relazioni
    public ICollection<Bolletta> Bollette { get; set; } = new List<Bolletta>();
    public ICollection<LetturaContatore> Letture { get; set; } = new List<LetturaContatore>();

    /// <summary>PDF del contratto conservato cifrato (1:1, opzionale).</summary>
    public DocumentoContratto? Documento { get; set; }
}
