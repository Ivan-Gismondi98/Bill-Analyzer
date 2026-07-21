using BollettaAnalyzer.Application.Common.Exceptions;
using Tesseract;

namespace BollettaAnalyzer.Infrastructure.Services.Ocr;

/// <summary>
/// Motore OCR reale basato su Tesseract, per estrarre testo da immagini di bollette
/// (foto/scansioni JPG, PNG, TIFF, BMP). Richiede i dati di lingua (es. "ita.traineddata")
/// nella cartella configurata in <see cref="TesseractSettings.DataPath"/>.
///
/// Nota di deployment: il pacchetto Tesseract include i binari nativi per Windows; su Linux
/// serve installare le librerie di sistema (libtesseract/libleptonica) e fornire i traineddata.
/// </summary>
public class TesseractOcrEngine
{
    private readonly TesseractSettings _settings;

    public TesseractOcrEngine(OcrSettings settings)
    {
        _settings = settings.Tesseract;
    }

    /// <summary>
    /// Esegue l'OCR su un'immagine in memoria e restituisce testo e confidenza media (0..1).
    /// </summary>
    public (string testo, decimal confidenza) Riconosci(byte[] immagine)
    {
        if (!Directory.Exists(_settings.DataPath))
            throw new OcrParsingException(
                $"Dati Tesseract non trovati in '{_settings.DataPath}'. " +
                "Installa i file di lingua (es. ita.traineddata) o usa il provider OCR 'Azure'.");

        try
        {
            using var engine = new TesseractEngine(_settings.DataPath, _settings.Language, EngineMode.Default);
            using var pix = Pix.LoadFromMemory(immagine);
            using var page = engine.Process(pix);

            var testo = page.GetText() ?? string.Empty;
            var confidenza = (decimal)page.GetMeanConfidence(); // già normalizzata 0..1
            return (testo, confidenza);
        }
        catch (OcrParsingException) { throw; }
        catch (Exception ex)
        {
            throw new OcrParsingException("Errore durante l'OCR dell'immagine con Tesseract.", ex);
        }
    }
}
