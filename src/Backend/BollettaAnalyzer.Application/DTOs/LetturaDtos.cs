namespace BollettaAnalyzer.Application.DTOs;

public record LetturaContatoreDto(
    Guid Id,
    Guid ContrattoId,
    DateTime DataLettura,
    decimal ValoreTotale,
    decimal? ValoreF1,
    decimal? ValoreF2,
    decimal? ValoreF3,
    bool DaBolletta,
    string? Note);

public record CreateLetturaRequest(
    Guid ContrattoId,
    DateTime DataLettura,
    decimal ValoreTotale,
    decimal? ValoreF1,
    decimal? ValoreF2,
    decimal? ValoreF3,
    string? Note);
