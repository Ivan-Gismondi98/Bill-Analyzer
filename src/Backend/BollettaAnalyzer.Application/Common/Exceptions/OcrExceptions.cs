namespace BollettaAnalyzer.Application.Common.Exceptions;

/// <summary>
/// Errore sollevato quando un documento non può essere letto/analizzato dal servizio OCR
/// (file corrotto, formato non supportato, PDF senza layer di testo e OCR non disponibile,
/// provider remoto non configurato, ecc.). È un errore "di dominio applicativo": la API lo
/// traduce in una risposta 422 con messaggio leggibile, senza esporre uno stack trace.
/// </summary>
public class OcrParsingException : Exception
{
    public OcrParsingException(string message) : base(message) { }
    public OcrParsingException(string message, Exception innerException) : base(message, innerException) { }
}
