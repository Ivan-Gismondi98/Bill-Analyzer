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
    string Nome,
    int PotenzaWatt,
    decimal OreUtilizzoGiornaliere,
    int GiorniSettimana,
    FasciaOraria FasciaPrevalente);
