using BollettaAnalyzer.Application.DTOs;
using BollettaAnalyzer.Domain.Entities;

namespace BollettaAnalyzer.Application.Common.Interfaces;

/// <summary>Astrazione del servizio OCR/parsing bollette (mock o Azure Document Intelligence / Tesseract).</summary>
public interface IBillOcrService
{
    /// <summary>Estrae i dati strutturati da un documento (PDF/immagine) di bolletta.</summary>
    Task<OcrResultDto> EstraiDatiAsync(Stream documento, string nomeFile, CancellationToken ct = default);
}

/// <summary>Generatore di token JWT.</summary>
public interface IJwtTokenGenerator
{
    (string token, DateTime scadenza) GeneraToken(Utente utente);
}

/// <summary>Hashing e verifica password.</summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verifica(string password, string hash);
}

/// <summary>Contesto dell'utente autenticato corrente.</summary>
public interface ICurrentUser
{
    Guid? UtenteId { get; }
    bool IsAuthenticated { get; }
}

/// <summary>Motore di generazione dei suggerimenti di risparmio.</summary>
public interface ISuggerimentiService
{
    IReadOnlyList<SuggerimentoDto> GeneraSuggerimenti(Contratto contratto, Bolletta bolletta);
}

/// <summary>Calcoli di simulazione consumi e previsione bolletta.</summary>
public interface ISimulazioneService
{
    SimulazioneDispositiviResponse SimulaDispositivi(Contratto contratto, IReadOnlyList<DispositivoElettronico> dispositivi);

    PrevisioneBollettaResponse PrevediBolletta(
        Contratto contratto,
        LetturaContatore? ultimaLettura,
        PrevisioneBollettaRequest richiesta);
}
