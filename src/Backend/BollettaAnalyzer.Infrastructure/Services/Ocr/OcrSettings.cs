namespace BollettaAnalyzer.Infrastructure.Services.Ocr;

/// <summary>
/// Impostazioni del sottosistema OCR (bind da appsettings: sezione "Ocr").
/// </summary>
public class OcrSettings
{
    /// <summary>
    /// Provider da usare: "Local" (PdfPig + Tesseract), "Azure" (Document Intelligence)
    /// oppure "Mock" (dati fittizi per sviluppo). Default: "Local".
    /// </summary>
    public string Provider { get; set; } = "Local";

    /// <summary>
    /// Lunghezza minima (in caratteri) del testo estratto da un PDF perché sia considerato
    /// un PDF "digitale" con layer di testo. Sotto questa soglia il documento è trattato come
    /// scansione: nella pipeline Local si tenta l'OCR raster, altrimenti si segnala l'errore.
    /// </summary>
    public int MinPdfTextLength { get; set; } = 40;

    public TesseractSettings Tesseract { get; set; } = new();
    public AzureDocIntelSettings Azure { get; set; } = new();
}

/// <summary>Impostazioni del motore OCR locale Tesseract.</summary>
public class TesseractSettings
{
    /// <summary>Cartella contenente i file *.traineddata (es. "ita.traineddata").</summary>
    public string DataPath { get; set; } = "./tessdata";

    /// <summary>Lingue Tesseract (formato "ita" oppure "ita+eng").</summary>
    public string Language { get; set; } = "ita";
}

/// <summary>Impostazioni del provider Azure AI Document Intelligence.</summary>
public class AzureDocIntelSettings
{
    /// <summary>Endpoint della risorsa, es. "https://&lt;nome&gt;.cognitiveservices.azure.com/".</summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>Chiave API della risorsa (impostare via secret/variabile d'ambiente in produzione).</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Modello da usare. Default: modello prebuilt per fatture.</summary>
    public string ModelId { get; set; } = "prebuilt-invoice";
}
