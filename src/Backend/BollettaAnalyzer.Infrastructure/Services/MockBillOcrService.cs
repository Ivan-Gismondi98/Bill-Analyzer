using BollettaAnalyzer.Application.Common.Interfaces;
using BollettaAnalyzer.Application.DTOs;
using BollettaAnalyzer.Domain.Enums;

namespace BollettaAnalyzer.Infrastructure.Services;

/// <summary>
/// Servizio OCR fittizio: restituisce dati plausibili senza analizzare realmente il documento.
/// Pronto per essere sostituito da un'integrazione reale (Azure Document Intelligence / Tesseract)
/// implementando la stessa interfaccia <see cref="IBillOcrService"/>.
/// </summary>
public class MockBillOcrService : IBillOcrService
{
    public async Task<OcrResultDto> EstraiDatiAsync(Stream documento, string nomeFile, CancellationToken ct = default)
    {
        // Simula la latenza di un servizio OCR remoto.
        await Task.Delay(400, ct);

        // Deriva valori "casuali ma stabili" dalla lunghezza del file per rendere il mock deterministico.
        long dimensione = documento.CanSeek ? documento.Length : 512_000;
        var seme = (int)(dimensione % 200);

        decimal consumoTotale = 320 + seme;
        decimal f1 = Math.Round(consumoTotale * 0.48m, 2);
        decimal f2 = Math.Round(consumoTotale * 0.22m, 2);
        decimal f3 = Math.Round(consumoTotale - f1 - f2, 2);

        var materia = Math.Round(consumoTotale * 0.135m, 2);
        var trasporto = Math.Round(consumoTotale * 0.045m, 2);
        var oneri = Math.Round(consumoTotale * 0.030m, 2);
        var imposte = Math.Round((materia + trasporto + oneri) * 0.12m, 2);

        var voci = new List<VoceDiCostoDto>
        {
            new(CategoriaCosto.MateriaEnergia, "Spesa per la materia energia", materia),
            new(CategoriaCosto.TrasportoGestione, "Spesa trasporto e gestione contatore", trasporto),
            new(CategoriaCosto.OneriDiSistema, "Oneri di sistema", oneri),
            new(CategoriaCosto.Imposte, "Imposte e IVA", imposte),
        };

        var fine = new DateTime(2026, 4, 30);
        var inizio = new DateTime(2026, 3, 1);

        return new OcrResultDto(
            NumeroFattura: $"MOCK-{seme:D4}-2026",
            PeriodoInizio: inizio,
            PeriodoFine: fine,
            ImportoTotale: voci.Sum(v => v.Importo),
            ConsumoTotaleKwh: consumoTotale,
            ConsumoF1Kwh: f1,
            ConsumoF2Kwh: f2,
            ConsumoF3Kwh: f3,
            ConsumoSm3: 0m,
            VociDiCosto: voci,
            ConfidenzaMedia: 0.92m);
    }
}
