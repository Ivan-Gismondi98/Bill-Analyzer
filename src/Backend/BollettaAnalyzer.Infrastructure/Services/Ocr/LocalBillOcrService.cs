using BollettaAnalyzer.Application.Common.Exceptions;
using BollettaAnalyzer.Application.Common.Interfaces;
using BollettaAnalyzer.Application.DTOs;

namespace BollettaAnalyzer.Infrastructure.Services.Ocr;

/// <summary>
/// Servizio OCR "locale" (nessun servizio cloud richiesto):
///  1. PDF con layer di testo  → estrazione diretta con PdfPig (nessun OCR necessario);
///  2. Immagini (JPG/PNG/TIFF/BMP) → OCR reale con Tesseract;
///  3. il testo ottenuto viene poi analizzato da <see cref="ItalianBillParser"/> per ricavare
///     importo, periodo, consumi per fascia, Smc e voci di costo ARERA.
///
/// I PDF scansionati (senza testo) non sono supportati da questa pipeline senza un
/// rasterizzatore: in quel caso viene sollevato un errore chiaro che suggerisce di caricare
/// un'immagine o di usare il provider 'Azure'.
/// </summary>
public class LocalBillOcrService : IBillOcrService
{
    private static readonly string[] EstensioniImmagine = { ".jpg", ".jpeg", ".png", ".tif", ".tiff", ".bmp", ".gif" };

    private readonly PdfTextExtractor _pdf;
    private readonly TesseractOcrEngine _tesseract;
    private readonly ItalianBillParser _parser;
    private readonly OcrSettings _settings;

    public LocalBillOcrService(
        PdfTextExtractor pdf, TesseractOcrEngine tesseract, ItalianBillParser parser, OcrSettings settings)
    {
        _pdf = pdf;
        _tesseract = tesseract;
        _parser = parser;
        _settings = settings;
    }

    public async Task<OcrResultDto> EstraiDatiAsync(Stream documento, string nomeFile, CancellationToken ct = default)
    {
        var bytes = await LeggiTuttoAsync(documento, ct);
        if (bytes.Length == 0)
            throw new OcrParsingException("Il file caricato è vuoto.");

        var estensione = Path.GetExtension(nomeFile).ToLowerInvariant();
        var isPdf = estensione == ".pdf" || IniziaConMagicPdf(bytes);
        var isImmagine = EstensioniImmagine.Contains(estensione);

        string testo;
        decimal? confidenzaMotore = null;

        if (isPdf)
        {
            testo = EstraiTestoDaPdf(bytes);
            if (testo.Trim().Length < _settings.MinPdfTextLength)
                throw new OcrParsingException(
                    "Il PDF sembra una scansione priva di testo. Carica una foto/immagine della bolletta " +
                    "oppure configura il provider OCR 'Azure' per l'analisi delle scansioni.");
        }
        else if (isImmagine)
        {
            var (t, conf) = _tesseract.Riconosci(bytes);
            testo = t;
            confidenzaMotore = conf;
        }
        else
        {
            // Formato ignoto: tentiamo PDF e, se fallisce, immagine.
            try
            {
                testo = EstraiTestoDaPdf(bytes);
            }
            catch
            {
                var (t, conf) = _tesseract.Riconosci(bytes);
                testo = t;
                confidenzaMotore = conf;
            }
        }

        if (string.IsNullOrWhiteSpace(testo))
            throw new OcrParsingException("Non è stato possibile estrarre testo dal documento.");

        return _parser.Parse(testo, confidenzaMotore);
    }

    private string EstraiTestoDaPdf(byte[] bytes)
    {
        try
        {
            return _pdf.EstraiTesto(bytes);
        }
        catch (OcrParsingException) { throw; }
        catch (Exception ex)
        {
            throw new OcrParsingException("Impossibile leggere il PDF (file non valido o protetto).", ex);
        }
    }

    private static bool IniziaConMagicPdf(byte[] bytes) =>
        bytes.Length >= 4 && bytes[0] == 0x25 && bytes[1] == 0x50 && bytes[2] == 0x44 && bytes[3] == 0x46; // %PDF

    private static async Task<byte[]> LeggiTuttoAsync(Stream s, CancellationToken ct)
    {
        if (s is MemoryStream ms) return ms.ToArray();
        using var buffer = new MemoryStream();
        await s.CopyToAsync(buffer, ct);
        return buffer.ToArray();
    }
}
