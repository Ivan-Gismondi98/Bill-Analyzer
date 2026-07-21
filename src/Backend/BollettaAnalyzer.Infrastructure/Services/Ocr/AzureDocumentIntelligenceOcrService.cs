using Azure;
using Azure.AI.DocumentIntelligence;
using BollettaAnalyzer.Application.Common.Exceptions;
using BollettaAnalyzer.Application.Common.Interfaces;
using BollettaAnalyzer.Application.DTOs;

namespace BollettaAnalyzer.Infrastructure.Services.Ocr;

/// <summary>
/// Provider OCR basato su Azure AI Document Intelligence (modello prebuilt "invoice").
/// Gestisce in modo robusto sia PDF digitali sia scansioni/foto.
///
/// Strategia ibrida: usa i campi strutturati del modello fattura (totale, numero, date) quando
/// disponibili e affidabili, e affida allo <see cref="ItalianBillParser"/> l'estrazione dei
/// campi tipici delle bollette energetiche (consumi kWh per fascia, Smc, voci ARERA) a partire
/// dal testo integrale riconosciuto (<c>AnalyzeResult.Content</c>).
/// </summary>
public class AzureDocumentIntelligenceOcrService : IBillOcrService
{
    private readonly AzureDocIntelSettings _cfg;
    private readonly ItalianBillParser _parser;

    public AzureDocumentIntelligenceOcrService(OcrSettings settings, ItalianBillParser parser)
    {
        _cfg = settings.Azure;
        _parser = parser;
    }

    public async Task<OcrResultDto> EstraiDatiAsync(Stream documento, string nomeFile, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_cfg.Endpoint) || string.IsNullOrWhiteSpace(_cfg.ApiKey))
            throw new OcrParsingException(
                "Provider OCR 'Azure' non configurato: impostare Ocr:Azure:Endpoint e Ocr:Azure:ApiKey.");

        AnalyzeResult result;
        try
        {
            var client = new DocumentIntelligenceClient(new Uri(_cfg.Endpoint), new AzureKeyCredential(_cfg.ApiKey));
            var contenuto = await BinaryData.FromStreamAsync(documento, ct);
            var opzioni = new AnalyzeDocumentOptions(_cfg.ModelId, contenuto);

            Operation<AnalyzeResult> operazione = await client.AnalyzeDocumentAsync(WaitUntil.Completed, opzioni, ct);

            result = operazione.Value;
        }
        catch (Exception ex)
        {
            throw new OcrParsingException("Errore durante l'analisi del documento con Azure Document Intelligence.", ex);
        }

        // 1) Parsing di dominio sul testo integrale riconosciuto da Azure.
        var baseResult = _parser.Parse(result.Content, ConfidenzaDocumento(result));

        // 2) Override con i campi strutturati del modello fattura, quando presenti e migliori.
        var numero = baseResult.NumeroFattura;
        var inizio = baseResult.PeriodoInizio;
        var fine = baseResult.PeriodoFine;
        var importo = baseResult.ImportoTotale;

        if (result.Documents is { Count: > 0 })
        {
            var campi = result.Documents[0].Fields;

            if (LeggiStringa(campi, "InvoiceId") is { } n && !string.IsNullOrWhiteSpace(n))
                numero = n;

            if (LeggiData(campi, "ServiceStartDate") is { } d1) inizio = d1;
            if (LeggiData(campi, "ServiceEndDate") is { } d2) fine = d2;

            if (LeggiValuta(campi, "InvoiceTotal") is { } tot && tot > 0)
                importo = tot;
        }

        if (fine < inizio) (inizio, fine) = (fine, inizio);

        return baseResult with
        {
            NumeroFattura = numero,
            PeriodoInizio = inizio,
            PeriodoFine = fine,
            ImportoTotale = importo,
        };
    }

    private static decimal? ConfidenzaDocumento(AnalyzeResult result)
    {
        try
        {
            if (result.Documents is { Count: > 0 })
                return (decimal)result.Documents[0].Confidence;
        }
        catch { /* campo non disponibile: si usa la sola confidenza di parsing */ }
        return null;
    }

    private static string? LeggiStringa(IReadOnlyDictionary<string, DocumentField> campi, string chiave)
    {
        try
        {
            if (campi.TryGetValue(chiave, out var f) && f.FieldType == DocumentFieldType.String)
                return f.ValueString;
        }
        catch { /* ignora differenze di tipo */ }
        return null;
    }

    private static DateTime? LeggiData(IReadOnlyDictionary<string, DocumentField> campi, string chiave)
    {
        try
        {
            if (campi.TryGetValue(chiave, out var f) && f.FieldType == DocumentFieldType.Date && f.ValueDate is { } d)
                return d.DateTime;
        }
        catch { /* ignora differenze di tipo */ }
        return null;
    }

    private static decimal? LeggiValuta(IReadOnlyDictionary<string, DocumentField> campi, string chiave)
    {
        try
        {
            if (campi.TryGetValue(chiave, out var f) && f.FieldType == DocumentFieldType.Currency)
                return (decimal)f.ValueCurrency.Amount;
        }
        catch { /* ignora differenze di tipo */ }
        return null;
    }
}
