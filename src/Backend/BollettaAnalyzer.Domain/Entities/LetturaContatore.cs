using BollettaAnalyzer.Domain.Common;
using BollettaAnalyzer.Domain.Enums;

namespace BollettaAnalyzer.Domain.Entities;

/// <summary>
/// Auto-lettura del contatore fisico inserita dall'utente.
/// Serve come base per la previsione della prossima bolletta.
/// </summary>
public class LetturaContatore : BaseEntity
{
    public Guid ContrattoId { get; set; }
    public Contratto? Contratto { get; set; }

    public DateTime DataLettura { get; set; } = DateTime.UtcNow;

    /// <summary>Lettura totale del contatore (kWh per luce, Sm3 per gas).</summary>
    public decimal ValoreTotale { get; set; }

    // Dettaglio per fascia (solo luce multioraria)
    public decimal? ValoreF1 { get; set; }
    public decimal? ValoreF2 { get; set; }
    public decimal? ValoreF3 { get; set; }

    /// <summary>Se true è la lettura ufficiale estratta dall'ultima bolletta salvata.</summary>
    public bool DaBolletta { get; set; }

    public string? Note { get; set; }
}
