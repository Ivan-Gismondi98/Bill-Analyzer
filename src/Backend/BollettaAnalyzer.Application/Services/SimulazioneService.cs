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
            var prezzo = PrezzoPerFascia(contratto, d.FasciaPrevalente);
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

    private static decimal PrezzoPerFascia(Contratto c, FasciaOraria fascia)
    {
        if (c.TipoTariffa == TipoTariffa.Monoraria || fascia == FasciaOraria.NonApplicabile)
            return c.PrezzoKwhMonorario > 0 ? c.PrezzoKwhMonorario : PrezzoMedioLuce(c);

        return fascia switch
        {
            FasciaOraria.F1 => c.PrezzoKwhF1,
            FasciaOraria.F2 => c.PrezzoKwhF2,
            FasciaOraria.F3 => c.PrezzoKwhF3,
            _ => PrezzoMedioLuce(c)
        };
    }

    private static decimal PrezzoMedioLuce(Contratto c)
    {
        if (c.TipoTariffa == TipoTariffa.Monoraria && c.PrezzoKwhMonorario > 0)
            return c.PrezzoKwhMonorario;

        var prezzi = new[] { c.PrezzoKwhF1, c.PrezzoKwhF2, c.PrezzoKwhF3 }.Where(p => p > 0).ToArray();
        return prezzi.Length > 0 ? prezzi.Average() : c.PrezzoKwhMonorario;
    }
}
