namespace BollettaAnalyzer.Domain.Common;

/// <summary>
/// Entità base con identificativo e metadati di audit condivisi da tutte le entità di dominio.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
