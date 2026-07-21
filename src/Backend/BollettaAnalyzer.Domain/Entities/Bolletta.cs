using BollettaAnalyzer.Domain.Common;
using BollettaAnalyzer.Domain.Enums;

namespace BollettaAnalyzer.Domain.Entities;

/// <summary>
/// Bolletta caricata dall'utente ed elaborata (OCR/parsing).
/// Contiene il dettaglio dei consumi per fascia e il breakdown dei costi.
/// </summary>
public class Bolletta : BaseEntity
{
    public Guid ContrattoId { get; set; }
    public Contratto? Contratto { get; set; }

    public string? NumeroFattura { get; set; }
    public DateTime PeriodoInizio { get; set; }
    public DateTime PeriodoFine { get; set; }
    public DateTime? DataEmissione { get; set; }

    /// <summary>Importo totale della bolletta in €.</summary>
    public decimal ImportoTotale { get; set; }

    // --- Consumi (Luce in kWh) ---
    public decimal ConsumoTotaleKwh { get; set; }
    public decimal ConsumoF1Kwh { get; set; }
    public decimal ConsumoF2Kwh { get; set; }
    public decimal ConsumoF3Kwh { get; set; }

    // --- Consumi (Gas in Sm3) ---
    public decimal ConsumoSm3 { get; set; }

    /// <summary>Percorso/URL del file PDF/immagine originale.</summary>
    public string? FileOriginale { get; set; }

    /// <summary>Indica se i dati provengono da OCR (true) o inseriti a mano (false).</summary>
    public bool DaOcr { get; set; }

    /// <summary>Confidenza media (0..1) dell'estrazione OCR; null per inserimenti manuali.</summary>
    public decimal? ConfidenzaOcr { get; set; }

    // Relazioni
    public ICollection<VoceDiCosto> VociDiCosto { get; set; } = new List<VoceDiCosto>();
}

/// <summary>Singola voce del breakdown di spesa di una bolletta.</summary>
public class VoceDiCosto : BaseEntity
{
    public Guid BollettaId { get; set; }
    public Bolletta? Bolletta { get; set; }

    public CategoriaCosto Categoria { get; set; }
    public string Descrizione { get; set; } = string.Empty;
    public decimal Importo { get; set; }
}
