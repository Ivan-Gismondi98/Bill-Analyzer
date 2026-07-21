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
    decimal QuotaFissaMensile);
