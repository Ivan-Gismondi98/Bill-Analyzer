using BollettaAnalyzer.Domain.Enums;

namespace BollettaAnalyzer.Application.DTOs;

public record VoceDiCostoDto(
    CategoriaCosto Categoria,
    string Descrizione,
    decimal Importo);

public record BollettaDto(
    Guid Id,
    Guid ContrattoId,
    string? NumeroFattura,
    DateTime PeriodoInizio,
    DateTime PeriodoFine,
    DateTime? DataEmissione,
    decimal ImportoTotale,
    decimal ConsumoTotaleKwh,
    decimal ConsumoF1Kwh,
    decimal ConsumoF2Kwh,
    decimal ConsumoF3Kwh,
    decimal ConsumoSm3,
    bool DaOcr,
    decimal? ConfidenzaOcr,
    IReadOnlyList<VoceDiCostoDto> VociDiCosto);

/// <summary>Risultato dell'analisi di una bolletta: breakdown + ripartizione fasce.</summary>
public record AnalisiBollettaDto(
    BollettaDto Bolletta,
    IReadOnlyList<VoceDiCostoDto> BreakdownCosti,
    RipartizioneFasceDto RipartizioneFasce,
    IReadOnlyList<SuggerimentoDto> Suggerimenti);

public record RipartizioneFasceDto(
    decimal PercentualeF1,
    decimal PercentualeF2,
    decimal PercentualeF3);

/// <summary>Payload restituito dal servizio OCR dopo il parsing del documento.</summary>
public record OcrResultDto(
    string? NumeroFattura,
    DateTime PeriodoInizio,
    DateTime PeriodoFine,
    decimal ImportoTotale,
    decimal ConsumoTotaleKwh,
    decimal ConsumoF1Kwh,
    decimal ConsumoF2Kwh,
    decimal ConsumoF3Kwh,
    decimal ConsumoSm3,
    IReadOnlyList<VoceDiCostoDto> VociDiCosto,
    decimal ConfidenzaMedia);
