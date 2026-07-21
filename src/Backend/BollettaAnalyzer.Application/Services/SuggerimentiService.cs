using BollettaAnalyzer.Application.Common.Interfaces;
using BollettaAnalyzer.Application.DTOs;
using BollettaAnalyzer.Domain.Entities;
using BollettaAnalyzer.Domain.Enums;

namespace BollettaAnalyzer.Application.Services;

/// <summary>
/// Motore euristico che analizza contratto + bolletta e produce consigli di risparmio contestuali.
/// I valori di benchmark sono indicativi del mercato retail italiano e vanno aggiornati periodicamente.
/// </summary>
public class SuggerimentiService : ISuggerimentiService
{
    // Benchmark di mercato indicativi (aggiornabili da configurazione)
    private const decimal PrezzoMedioMercatoKwh = 0.130m;   // €/kWh materia energia
    private const decimal PrezzoMedioMercatoSm3 = 0.480m;   // €/Sm3 materia gas
    private const decimal SogliaF1Dominante = 0.45m;        // % oltre la quale conviene spostare i consumi

    public IReadOnlyList<SuggerimentoDto> GeneraSuggerimenti(Contratto contratto, Bolletta bolletta)
    {
        var consigli = new List<SuggerimentoDto>();

        if (contratto.TipoFornitura == TipoFornitura.Luce)
        {
            AnalizzaPrezzoMateriaPrima(contratto, bolletta, consigli);
            AnalizzaFasceOrarie(bolletta, contratto, consigli);
            AnalizzaPotenzaImpegnata(contratto, bolletta, consigli);
        }
        else
        {
            AnalizzaPrezzoGas(contratto, bolletta, consigli);
        }

        if (consigli.Count == 0)
        {
            consigli.Add(new SuggerimentoDto(
                "Consumi in linea con il mercato",
                "I tuoi consumi e i costi risultano nella media. Continua a monitorare la bolletta per cogliere eventuali variazioni.",
                PrioritaSuggerimento.Bassa, 0m, "check-circle"));
        }

        return consigli
            .OrderByDescending(c => c.Priorita)
            .ThenByDescending(c => c.RisparmioStimatoAnnuo)
            .ToList();
    }

    private static void AnalizzaPrezzoMateriaPrima(Contratto c, Bolletta b, List<SuggerimentoDto> consigli)
    {
        var prezzoEffettivo = c.TipoTariffa == TipoTariffa.Monoraria
            ? c.PrezzoKwhMonorario
            : Media(c.PrezzoKwhF1, c.PrezzoKwhF2, c.PrezzoKwhF3);

        if (prezzoEffettivo > PrezzoMedioMercatoKwh * 1.10m)
        {
            var risparmio = (prezzoEffettivo - PrezzoMedioMercatoKwh) * StimaConsumoAnnuo(b.ConsumoTotaleKwh, b);
            consigli.Add(new SuggerimentoDto(
                "Costo materia prima sopra la media",
                $"Paghi {prezzoEffettivo:0.000} €/kWh contro una media di mercato di {PrezzoMedioMercatoKwh:0.000} €/kWh. " +
                "Valuta un'offerta più conveniente per ridurre la spesa.",
                PrioritaSuggerimento.Alta, Math.Round(risparmio, 0), "trending-down"));
        }
    }

    private static void AnalizzaFasceOrarie(Bolletta b, Contratto c, List<SuggerimentoDto> consigli)
    {
        var totale = b.ConsumoF1Kwh + b.ConsumoF2Kwh + b.ConsumoF3Kwh;
        if (totale <= 0) return;

        var quotaF1 = b.ConsumoF1Kwh / totale;
        if (quotaF1 > SogliaF1Dominante && c.TipoTariffa != TipoTariffa.Monoraria)
        {
            var deltaPrezzo = c.PrezzoKwhF1 - c.PrezzoKwhF3;
            var kwhSpostabili = b.ConsumoF1Kwh * 0.25m; // ipotesi: 25% spostabile in F3
            var risparmio = deltaPrezzo * StimaConsumoAnnuo(kwhSpostabili, b);
            consigli.Add(new SuggerimentoDto(
                "Sposta i consumi nelle fasce economiche",
                $"Il {quotaF1:P0} dei tuoi consumi è in fascia F1 (la più cara). Programma lavatrice, lavastoviglie e " +
                "ricarica dei dispositivi in fascia F3 (sera/notte e festivi) per risparmiare.",
                PrioritaSuggerimento.Media, Math.Round(Math.Max(0, risparmio), 0), "clock"));
        }
    }

    private static void AnalizzaPotenzaImpegnata(Contratto c, Bolletta b, List<SuggerimentoDto> consigli)
    {
        // Se consumo medio basso ma potenza alta, il costo fisso potenza incide troppo.
        var giorni = Math.Max(1, (b.PeriodoFine - b.PeriodoInizio).Days);
        var mediaGiornaliera = b.ConsumoTotaleKwh / giorni;
        if (c.PotenzaImpegnataKw > 3.5m && mediaGiornaliera < 6m)
        {
            consigli.Add(new SuggerimentoDto(
                "Potenza impegnata sovradimensionata",
                $"Hai {c.PotenzaImpegnataKw:0.0} kW impegnati ma un consumo medio contenuto. " +
                "Ridurre la potenza a 3 kW abbatte la quota fissa annua.",
                PrioritaSuggerimento.Media, 60m, "gauge"));
        }
    }

    private static void AnalizzaPrezzoGas(Contratto c, Bolletta b, List<SuggerimentoDto> consigli)
    {
        if (c.PrezzoSm3 > PrezzoMedioMercatoSm3 * 1.10m)
        {
            var risparmio = (c.PrezzoSm3 - PrezzoMedioMercatoSm3) * StimaConsumoAnnuo(b.ConsumoSm3, b);
            consigli.Add(new SuggerimentoDto(
                "Costo gas sopra la media",
                $"Paghi {c.PrezzoSm3:0.000} €/Sm3 contro una media di {PrezzoMedioMercatoSm3:0.000} €/Sm3. " +
                "Confronta altre offerte per il gas.",
                PrioritaSuggerimento.Alta, Math.Round(risparmio, 0), "flame"));
        }
    }

    private static decimal Media(params decimal[] valori) =>
        valori.Where(v => v > 0).DefaultIfEmpty(0).Average();

    private static decimal StimaConsumoAnnuo(decimal consumoPeriodo, Bolletta b)
    {
        var giorni = Math.Max(1, (b.PeriodoFine - b.PeriodoInizio).Days);
        return consumoPeriodo / giorni * 365m;
    }
}
