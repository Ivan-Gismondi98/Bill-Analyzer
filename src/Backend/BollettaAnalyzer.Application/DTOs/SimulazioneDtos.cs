namespace BollettaAnalyzer.Application.DTOs;

/// <summary>Livello di priorità di un suggerimento di risparmio.</summary>
public enum PrioritaSuggerimento { Bassa, Media, Alta }

public record SuggerimentoDto(
    string Titolo,
    string Descrizione,
    PrioritaSuggerimento Priorita,
    decimal RisparmioStimatoAnnuo,
    string Icona);

/// <summary>Richiesta di simulazione impatto elettrodomestici.</summary>
public record SimulazioneDispositiviRequest(
    Guid ContrattoId,
    IReadOnlyList<Guid> DispositiviIds);

public record SimulazioneDispositiviResponse(
    decimal ConsumoTotaleGiornalieroKwh,
    decimal ConsumoTotaleMensileKwh,
    decimal CostoStimatoMensile,
    decimal CostoStimatoAnnuo,
    IReadOnlyList<DettaglioDispositivoDto> Dettagli);

public record DettaglioDispositivoDto(
    string Nome,
    decimal ConsumoMensileKwh,
    decimal CostoMensile);

/// <summary>
/// Richiesta di previsione della prossima bolletta a partire da un'auto-lettura.
/// </summary>
public record PrevisioneBollettaRequest(
    Guid ContrattoId,
    decimal LetturaAttuale,
    DateTime DataLetturaAttuale,
    decimal? LetturaAttualeF1,
    decimal? LetturaAttualeF2,
    decimal? LetturaAttualeF3);

public record PrevisioneBollettaResponse(
    decimal ConsumoStimatoKwhOSm3,
    int GiorniPeriodo,
    decimal MediaGiornaliera,
    decimal CostoMateriaPrima,
    decimal QuotaFissa,
    decimal ImposteStimate,
    decimal ImportoStimatoTotale,
    DateTime DataProssimaBollettaStimata,
    string Note);
