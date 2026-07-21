using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace BollettaAnalyzer.Infrastructure.Services.Ocr;

/// <summary>
/// Estrae il testo dal layer testuale di un PDF "digitale" tramite PdfPig (100% gestito,
/// nessuna dipendenza nativa). Le bollette inviate in formato elettronico sono quasi sempre
/// PDF con testo selezionabile: in questo caso non serve OCR raster, basta leggere il testo.
///
/// Se il PDF è una scansione (solo immagini), il testo estratto sarà vuoto/scarno: spetta al
/// chiamante decidere il fallback (OCR Tesseract o provider Azure).
/// </summary>
public class PdfTextExtractor
{
    /// <summary>Restituisce il testo del PDF, pagina per pagina, rispettando l'ordine di lettura.</summary>
    public string EstraiTesto(byte[] pdf)
    {
        var sb = new StringBuilder();
        using var documento = PdfDocument.Open(pdf);
        foreach (var pagina in documento.GetPages())
        {
            // ContentOrderTextExtractor ricostruisce meglio spazi e a-capo rispetto a Page.Text.
            sb.AppendLine(ContentOrderTextExtractor.GetText(pagina));
            sb.AppendLine();
        }
        return sb.ToString();
    }
}
