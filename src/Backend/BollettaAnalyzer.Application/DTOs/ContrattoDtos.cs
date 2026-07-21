using System.ComponentModel.DataAnnotations;
using BollettaAnalyzer.Domain.Enums;

namespace BollettaAnalyzer.Application.DTOs;

public record ContrattoDto(
    Guid Id,
    TipoFornitura TipoFornitura,
    string Fornitore,
    string? CodicePod,
    string? CodicePdr,
    string NomeOfferta,
    TipoTariffa TipoTariffa,
    decimal PotenzaImpegnataKw,
    decimal PrezzoKwhMonorario,
    decimal PrezzoKwhF1,
    decimal PrezzoKwhF2,
    decimal PrezzoKwhF3,
    decimal PrezzoSm3,
    decimal QuotaFissaMensile,
    DateTime DataAttivazione,
    bool Attivo);

/// <summary>Metadati (senza contenuto) del PDF di contratto conservato cifrato.</summary>
public record DocumentoContrattoInfoDto(
    Guid ContrattoId,
    string NomeFile,
    string ContentType,
    long DimensioneByte,
    DateTime CaricatoIl);

public record UpsertContrattoRequest(
    TipoFornitura TipoFornitura,
    [property: Required, MaxLength(160)] string Fornitore,
    [property: MaxLength(30)] string? CodicePod,
    [property: MaxLength(30)] string? CodicePdr,
    [property: MaxLength(160)] string NomeOfferta,
    TipoTariffa TipoTariffa,
    [property: Range(0, 1000)] decimal PotenzaImpegnataKw,
    [property: Range(0, 100)] decimal PrezzoKwhMonorario,
    [property: Range(0, 100)] decimal PrezzoKwhF1,
    [property: Range(0, 100)] decimal PrezzoKwhF2,
    [property: Range(0, 100)] decimal PrezzoKwhF3,
    [property: Range(0, 100)] decimal PrezzoSm3,
    [property: Range(0, 10_000)] decimal QuotaFissaMensile);
