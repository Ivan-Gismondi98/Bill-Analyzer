using System.ComponentModel.DataAnnotations;

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
    [property: Range(0, 99_999_999)] decimal ValoreTotale,
    [property: Range(0, 99_999_999)] decimal? ValoreF1,
    [property: Range(0, 99_999_999)] decimal? ValoreF2,
    [property: Range(0, 99_999_999)] decimal? ValoreF3,
    [property: MaxLength(500)] string? Note);
