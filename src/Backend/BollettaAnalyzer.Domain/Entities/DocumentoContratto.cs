using BollettaAnalyzer.Domain.Common;

namespace BollettaAnalyzer.Domain.Entities;

/// <summary>
/// PDF del contratto luce/gas caricato dall'utente, conservato <b>cifrato a riposo</b> (AES-GCM).
/// Relazione 1:1 con <see cref="Contratto"/>.
/// </summary>
public class DocumentoContratto : BaseEntity
{
    public Guid ContrattoId { get; set; }
    public Contratto? Contratto { get; set; }

    public string NomeFile { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/pdf";
    public long DimensioneByte { get; set; }

    // Materiale cifrato (mai in chiaro nel database)
    public byte[] Contenuto { get; set; } = Array.Empty<byte>();
    public byte[] Nonce { get; set; } = Array.Empty<byte>();
    public byte[] Tag { get; set; } = Array.Empty<byte>();
}
