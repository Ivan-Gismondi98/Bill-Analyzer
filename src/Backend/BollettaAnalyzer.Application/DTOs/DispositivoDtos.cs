using System.ComponentModel.DataAnnotations;
using BollettaAnalyzer.Domain.Enums;

namespace BollettaAnalyzer.Application.DTOs;

public record DispositivoDto(
    Guid Id,
    string Nome,
    int PotenzaWatt,
    decimal OreUtilizzoGiornaliere,
    int GiorniSettimana,
    IReadOnlyList<FasciaOraria> Fasce,
    decimal ConsumoGiornalieroKwh,
    decimal ConsumoMensileKwh);

public record UpsertDispositivoRequest(
    [Required, MaxLength(120)] string Nome,
    [Range(1, 50_000)] int PotenzaWatt,
    [Range(0, 24)] decimal OreUtilizzoGiornaliere,
    [Range(1, 7)] int GiorniSettimana,
    // Fasce orarie di utilizzo: una o più tra F1/F2/F3 ("sempre attivo" = tutte e tre).
    [Required, MinLength(1)] IReadOnlyList<FasciaOraria> Fasce);
