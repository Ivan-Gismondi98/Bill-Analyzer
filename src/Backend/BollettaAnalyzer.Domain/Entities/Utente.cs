using BollettaAnalyzer.Domain.Common;

namespace BollettaAnalyzer.Domain.Entities;

/// <summary>Utente registrato dell'applicazione.</summary>
public class Utente : BaseEntity
{
    public string Email { get; set; } = string.Empty;

    /// <summary>Hash della password (mai in chiaro).</summary>
    public string PasswordHash { get; set; } = string.Empty;

    public string Nome { get; set; } = string.Empty;
    public string Cognome { get; set; } = string.Empty;
    public string? Telefono { get; set; }

    /// <summary>Indirizzo di fornitura principale.</summary>
    public string? Indirizzo { get; set; }
    public string? Citta { get; set; }
    public string? Cap { get; set; }

    // Relazioni
    public ICollection<Contratto> Contratti { get; set; } = new List<Contratto>();
    public ICollection<DispositivoElettronico> Dispositivi { get; set; } = new List<DispositivoElettronico>();
}
