using System.Globalization;
using System.Text.RegularExpressions;
using BollettaAnalyzer.Application.DTOs;
using BollettaAnalyzer.Domain.Enums;

namespace BollettaAnalyzer.Infrastructure.Services.Ocr;

/// <summary>
/// Parser euristico per i CONTRATTI di fornitura italiani (luce/gas): dal testo del PDF
/// ricava fornitore, POD/PDR, prezzi (€/kWh mono o per fascia, €/Smc), quota fissa e
/// potenza impegnata. Tutti i campi sono opzionali: il risultato serve solo a
/// precompilare il form del contratto, che l'utente rivede e salva. Il PDF non viene
/// mai conservato.
/// </summary>
public class ItalianContractParser
{
    private const string Num = @"(\d{1,3}(?:[.\s]\d{3})*(?:,\d+)?|\d+(?:[.,]\d+)?)";

    private static readonly string[] FornitoriNoti =
    {
        "Enel Energia", "Servizio Elettrico Nazionale", "Eni Plenitude", "Plenitude",
        "Edison", "A2A", "Iren", "Hera", "Acea", "Sorgenia", "Engie", "Illumia",
        "Octopus", "NeN", "Wekiwi", "E.ON", "Dolomiti Energia", "Alperia", "AGSM",
        "Estra", "Duferco", "Pulsee", "Tate", "Eni gas e luce"
    };

    public ContrattoEstrattoDto Parse(string testo)
    {
        var t = Regex.Replace(testo ?? string.Empty, @"[ \t]{2,}", " ");

        var pod = Match1(t, @"\b(IT\d{3}E\d{8,10})\b");
        var pdr = Match1(t, @"\bPDR\s*[:\-]?\s*(\d{14})\b") ?? Match1(t, @"\b(\d{14})\b");

        var f1 = Prezzo(t, @"F1\s*[:\-]?\s*€?\s*" + Num + @"\s*€?\s*/?\s*kwh");
        var f2 = Prezzo(t, @"F2\s*[:\-]?\s*€?\s*" + Num + @"\s*€?\s*/?\s*kwh");
        var f3 = Prezzo(t, @"F3\s*[:\-]?\s*€?\s*" + Num + @"\s*€?\s*/?\s*kwh");
        var mono = Prezzo(t, @"(?:prezzo\s+(?:energia|luce|monorario)|corrispettivo\s+energia)[\s\S]{0,40}?" + Num + @"\s*€?\s*/?\s*kwh")
                   ?? Prezzo(t, Num + @"\s*€\s*/\s*kwh");
        var gas = Prezzo(t, @"(?:prezzo\s+(?:gas|materia\s+prima)|corrispettivo\s+gas)[\s\S]{0,40}?" + Num + @"\s*€?\s*/?\s*(?:smc|sm3|sm³)")
                  ?? Prezzo(t, Num + @"\s*€\s*/\s*(?:smc|sm3|sm³)");

        // Quota fissa: €/mese diretti, oppure €/anno da dividere per 12.
        var quotaMese = Prezzo(t, @"(?:quota\s+fissa|commercializzazione)[\s\S]{0,50}?" + Num + @"\s*€?\s*/?\s*mese");
        var quotaAnno = Prezzo(t, @"(?:quota\s+fissa|commercializzazione)[\s\S]{0,50}?" + Num + @"\s*€?\s*/?\s*anno");
        var quota = quotaMese ?? (quotaAnno is not null ? Math.Round(quotaAnno.Value / 12m, 2) : null);

        var potenza = Prezzo(t, @"potenza\s+(?:impegnata|contrattuale)[\s\S]{0,30}?" + Num + @"\s*kw");

        var fornitore = FornitoriNoti.FirstOrDefault(f => t.Contains(f, StringComparison.OrdinalIgnoreCase));
        var offerta = Match1(t, @"(?:offerta|nome\s+offerta)\s*[:\-]\s*([^\r\n]{3,60})")?.Trim();

        // Tipo di fornitura e tariffa dedotti dai campi trovati.
        TipoFornitura? tipo = (f1 ?? f2 ?? f3 ?? mono) is not null ? TipoFornitura.Luce
                            : gas is not null ? TipoFornitura.Gas
                            : pod is not null ? TipoFornitura.Luce
                            : pdr is not null ? TipoFornitura.Gas
                            : null;

        TipoTariffa? tariffa = tipo != TipoFornitura.Luce ? null
                             : (f1 is not null && f2 is not null && f3 is not null) ? TipoTariffa.Multioraria
                             : (f1 is not null && (f2 ?? f3) is not null) ? TipoTariffa.Bioraria
                             : mono is not null ? TipoTariffa.Monoraria
                             : null;

        // Confidenza: quota di campi chiave effettivamente trovati.
        var chiave = new object?[] { tipo, fornitore, pod ?? pdr, f1 ?? mono ?? gas, quota };
        var confidenza = Math.Round((decimal)chiave.Count(x => x is not null) / chiave.Length, 2);

        return new ContrattoEstrattoDto(
            tipo, fornitore, pod, pdr, offerta, tariffa,
            potenza, mono, f1, f2, f3, gas, quota, confidenza);
    }

    private static string? Match1(string t, string pattern)
    {
        var m = Regex.Match(t, pattern, RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value : null;
    }

    private static decimal? Prezzo(string t, string pattern)
    {
        var raw = Match1(t, pattern);
        if (raw is null) return null;
        var s = raw.Replace(" ", "");
        // Formato italiano: virgola decimale, punto migliaia; ma nei prezzi unitari
        // il punto è quasi sempre decimale (0.123): si gestiscono entrambi.
        if (s.Contains(',')) s = s.Replace(".", "").Replace(',', '.');
        return decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) && d >= 0 ? d : null;
    }
}
