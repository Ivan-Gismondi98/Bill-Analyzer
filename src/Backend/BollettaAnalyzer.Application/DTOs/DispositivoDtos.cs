using System.ComponentModel.DataAnnotations;
using BollettaAnalyzer.Domain.Enums;

namespace BollettaAnalyzer.Application.DTOs;

public record DispositivoDto(
    Guid Id,
    string Nome,
    int PotenzaWatt,
    decimal OreUtilizzoGiornaliere,
    int GiorniSettimana,
    FasciaOraria FasciaPrevalente,
    decimal ConsumoGiornalieroKwh,
    decimal ConsumoMensileKwh);

public record UpsertDispositivoRequest(
    [property: Required, MaxLength(120)] string Nome,
    [property: Range(1, 50_000)] int PotenzaWatt,
    [property: Range(0, 24)] decimal OreUtilizzoGiornaliere,
    [property: Range(1, 7)] int GiorniSettimana,
    FasciaOraria FasciaPrevalente);
