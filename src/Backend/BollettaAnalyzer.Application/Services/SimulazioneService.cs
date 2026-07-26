using BollettaAnalyzer.Application.Common.Interfaces;
using BollettaAnalyzer.Application.DTOs;
using BollettaAnalyzer.Domain.Entities;
using BollettaAnalyzer.Domain.Enums;

namespace BollettaAnalyzer.Application.Services;

/// <summary>
/// Calcoli di simulazione: impatto degli elettrodomestici e previsione della prossima bolletta
/// a partire dall'auto-lettura del contatore.
/// </summary>
public class SimulazioneService : ISimulazioneService
{
    // Componenti fiscali indicative sull'energia elettrica.
    private const decimal AliquotaIva = 0.10m;      // IVA 10% (uso domestico)
    private const decimal AccisaKwh = 0.0227m;      // €/kWh accisa energia
    private const int GiorniPeriodoStandard = 60;   // bolletta bimestrale tipica

    public SimulazioneDispositiviResponse SimulaDispositivi(
        Contratto contratto,
        IReadOnlyList<DispositivoElettronico> dispositivi)
    {
        var dettagli = new List<DettaglioDispositivoDto>();
        decimal totaleGiornaliero = 0, totaleMensile = 0, costoMensile = 0;

        foreach (var d in dispositivi)
        {
            var prezzo = PrezzoMedioPerFasce(contratto, d);
            var costo = d.ConsumoMensileKwh * prezzo;

            totaleGiornaliero += d.ConsumoGiornalieroKwh;
            totaleMensile += d.ConsumoMensileKwh;
            costoMensile += costo;

            dettagli.Add(new DettaglioDispositivoDto(
                d.Nome,
                Math.Round(d.ConsumoMensileKwh, 2),
                Math.Round(costo, 2)));
        }

        // Aggiunge la quota fissa mensile del contratto al costo simulato.
        costoMensile += contratto.QuotaFissaMensile;

        return new SimulazioneDispositiviResponse(
            Math.Round(totaleGiornaliero, 3),
            Math.Round(totaleMensile, 2),
            Math.Round(costoMensile, 2),
            Math.Round(costoMensile * 12, 2),
            dettagli.OrderByDescending(x => x.CostoMensile).ToList());
    }

    public PrevisioneBollettaResponse PrevediBolletta(
        Contratto contratto,
        LetturaContatore? ultimaLettura,
        PrevisioneBollettaRequest richiesta)
    {
        decimal consumoPeriodo;
        int giorni;
        string note;

        if (ultimaLettura is not null)
        {
            consumoPeriodo = Math.Max(0, richiesta.LetturaAttuale - ultimaLettura.ValoreTotale);
            giorni = Math.Max(1, (richiesta.DataLetturaAttuale.Date - ultimaLettura.DataLettura.Date).Days);
            note = $"Previsione basata sulla differenza di lettura dal {ultimaLettura.DataLettura:dd/MM/yyyy}.";
        }
        else
        {
            // Nessuna lettura precedente: assume che la lettura inserita sia già il consumo del periodo.
            consumoPeriodo = Math.Max(0, richiesta.LetturaAttuale);
            giorni = GiorniPeriodoStandard;
            note = "Nessuna lettura precedente trovata: stima basata sul valore inserito come consumo di periodo.";
        }

        var mediaGiornaliera = consumoPeriodo / giorni;
        var consumoProiettato = mediaGiornaliera * GiorniPeriodoStandard;

        var prezzoUnitario = contratto.TipoFornitura == TipoFornitura.Luce
            ? PrezzoMedioLuce(contratto)
            : contratto.PrezzoSm3;

        var costoMateriaPrima = consumoProiettato * prezzoUnitario;
        var quotaFissa = contratto.QuotaFissaMensile * (GiorniPeriodoStandard / 30m);

        var imponibile = costoMateriaPrima + quotaFissa;
        var accise = contratto.TipoFornitura == TipoFornitura.Luce ? consumoProiettato * AccisaKwh : 0m;
        var iva = (imponibile + accise) * AliquotaIva;
        var importoTotale = imponibile + accise + iva;

        return new PrevisioneBollettaResponse(
            Math.Round(consumoProiettato, 2),
            GiorniPeriodoStandard,
            Math.Round(mediaGiornaliera, 3),
            Math.Round(costoMateriaPrima, 2),
            Math.Round(quotaFissa, 2),
            Math.Round(accise + iva, 2),
            Math.Round(importoTotale, 2),
            richiesta.DataLetturaAttuale.AddDays(GiorniPeriodoStandard),
            note);
    }

    // Ore settimanali delle fasce ARERA (totale 168):
    // F1 = lun-ven 8-19 (55h) · F2 = lun-ven 7-8 e 19-23 + sab 7-23 (41h) · F3 = resto (72h).
    private const decimal OreF1 = 55m, OreF2 = 41m, OreF3 = 72m;

    /// <summary>
    /// Prezzo medio €/kWh per un dispositivo, ponderato sulle ore settimanali delle
    /// fasce in cui viene usato: un frigorifero "sempre attivo" (F1+F2+F3) paga la
    /// media pesata delle tre fasce, una lavatrice solo-F3 paga il prezzo F3.
    /// </summary>
    private static decimal PrezzoMedioPerFasce(Contratto c, DispositivoElettronico d)
    {
        if (c.TipoTariffa == TipoTariffa.Monoraria)
            return c.PrezzoKwhMonorario > 0 ? c.PrezzoKwhMonorario : PrezzoMedioLuce(c);

        decimal sommaPrezzo = 0m, sommaOre = 0m;
        if (d.UsaF1) { sommaPrezzo += c.PrezzoKwhF1 * OreF1; sommaOre += OreF1; }
        if (d.UsaF2) { sommaPrezzo += c.PrezzoKwhF2 * OreF2; sommaOre += OreF2; }
        if (d.UsaF3) { sommaPrezzo += c.PrezzoKwhF3 * OreF3; sommaOre += OreF3; }

        return sommaOre > 0 ? sommaPrezzo / sommaOre : PrezzoMedioLuce(c);
    }

    private static decimal PrezzoMedioLuce(Contratto c)
    {
        if (c.TipoTariffa == TipoTariffa.Monoraria && c.PrezzoKwhMonorario > 0)
            return c.PrezzoKwhMonorario;

        var prezzi = new[] { c.PrezzoKwhF1, c.PrezzoKwhF2, c.PrezzoKwhF3 }.Where(p => p > 0).ToArray();
        return prezzi.Length > 0 ? prezzi.Average() : c.PrezzoKwhMonorario;
    }
}
