using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using BollettaAnalyzer.Application.DTOs;
using BollettaAnalyzer.Domain.Enums;

namespace BollettaAnalyzer.Infrastructure.Services.Ocr;

/// <summary>
/// Parser di dominio per bollette italiane luce/gas. Riceve il testo grezzo estratto da
/// un documento (via PdfPig, Tesseract o Azure Document Intelligence) e ne ricava i campi
/// strutturati (<see cref="OcrResultDto"/>): importo totale, numero fattura, periodo di
/// competenza, consumi per fascia (F1/F2/F3), consumo gas in Smc e le voci di costo ARERA.
///
/// È volutamente indipendente dalla sorgente OCR: la stessa logica funziona su qualsiasi
/// testo, così l'accuratezza migliora insieme alla qualità dell'estrazione a monte.
/// </summary>
public class ItalianBillParser
{
    private static readonly CultureInfo It = CultureInfo.GetCultureInfo("it-IT");

    // Numero in formato italiano: "1.234,56", "1234,56", "342", "1 234,56".
    // Un solo gruppo di cattura, così l'indice del gruppo resta prevedibile quando lo si annida.
    private const string Num = @"(\d{1,3}(?:[.\s]\d{3})*(?:,\d+)?|\d+(?:,\d+)?)";

    // Data: gg/mm/aaaa con separatori / . - e anno a 2 o 4 cifre.
    private const string Data = @"(\d{1,2}[\/.\-]\d{1,2}[\/.\-]\d{2,4})";

    private static readonly string[] FormatiData =
    {
        "d/M/yyyy", "dd/MM/yyyy", "d-M-yyyy", "dd-MM-yyyy", "d.M.yyyy", "dd.MM.yyyy",
        "d/M/yy", "dd/MM/yy", "d-M-yy", "dd-MM-yy"
    };

    /// <summary>
    /// Analizza il testo e restituisce i campi strutturati.
    /// </summary>
    /// <param name="testo">Testo grezzo della bolletta.</param>
    /// <param name="confidenzaMotore">
    /// Confidenza del motore OCR a monte (0..1), se disponibile (es. Tesseract/Azure).
    /// Viene mediata con la confidenza "di parsing" basata sui campi effettivamente trovati.
    /// </param>
    public OcrResultDto Parse(string? testo, decimal? confidenzaMotore = null)
    {
        var t = Normalizza(testo ?? string.Empty);

        var (inizio, fine, periodoTrovato) = EstraiPeriodo(t);
        var numeroFattura = EstraiNumeroFattura(t);

        // Consumi elettrici per fascia.
        var f1 = EstraiKwhFascia(t, "1") ?? 0m;
        var f2 = EstraiKwhFascia(t, "2") ?? 0m;
        var f3 = EstraiKwhFascia(t, "3") ?? 0m;

        var totaleKwh = EstraiConsumoTotaleKwh(t);
        if (totaleKwh is null && (f1 + f2 + f3) > 0) totaleKwh = f1 + f2 + f3;

        // Consumo gas.
        var smc = EstraiConsumoSmc(t) ?? 0m;

        // Voci di costo ARERA.
        var voci = EstraiVociDiCosto(t);

        // Importo totale: label esplicita, altrimenti somma delle voci.
        var importo = EstraiImportoTotale(t) ?? (voci.Count > 0 ? voci.Sum(v => v.Importo) : 0m);

        // Se ho un totale ma nessuna voce, creo una voce sintetica così l'analisi a valle ha dati.
        if (voci.Count == 0 && importo > 0)
            voci = new List<VoceDiCostoDto> { new(CategoriaCosto.MateriaEnergia, "Importo totale bolletta", importo) };

        var confidenza = CalcolaConfidenza(
            importoTrovato: importo > 0,
            periodoTrovato: periodoTrovato,
            consumoTrovato: (totaleKwh ?? 0m) > 0 || smc > 0,
            fasceTrovate: (f1 + f2 + f3) > 0,
            vociTrovate: voci.Count > 0 && !(voci.Count == 1 && voci[0].Descrizione == "Importo totale bolletta"),
            numeroTrovato: !string.IsNullOrWhiteSpace(numeroFattura),
            confidenzaMotore: confidenzaMotore);

        return new OcrResultDto(
            NumeroFattura: numeroFattura,
            PeriodoInizio: inizio,
            PeriodoFine: fine,
            ImportoTotale: Math.Round(importo, 2),
            ConsumoTotaleKwh: Math.Round(totaleKwh ?? 0m, 2),
            ConsumoF1Kwh: Math.Round(f1, 2),
            ConsumoF2Kwh: Math.Round(f2, 2),
            ConsumoF3Kwh: Math.Round(f3, 2),
            ConsumoSm3: Math.Round(smc, 2),
            VociDiCosto: voci,
            ConfidenzaMedia: confidenza);
    }

    // ---------------------------------------------------------------------
    // Estrattori
    // ---------------------------------------------------------------------

    private static decimal? EstraiImportoTotale(string t)
    {
        // Ordinati per specificità: prima le etichette più affidabili.
        string[] labels =
        {
            @"totale\s+da\s+pagare",
            @"totale\s+bolletta",
            @"totale\s+fattura",
            @"importo\s+totale",
            @"totale\s+dovuto",
            @"totale\s+documento",
            @"totale\s+€",
        };

        foreach (var label in labels)
        {
            var v = ImportoVicino(t, label);
            if (v is > 0) return v;
        }
        return null;
    }

    private static string? EstraiNumeroFattura(string t)
    {
        var patterns = new[]
        {
            @"(?:fattura|bolletta)\s*(?:elettronica\s*)?(?:n[.°]?|numero|nr[.]?)\s*[:\-]?\s*([A-Z0-9][A-Z0-9\/\-]{3,})",
            @"n[.°]\s*fattura\s*[:\-]?\s*([A-Z0-9][A-Z0-9\/\-]{3,})",
            @"numero\s+documento\s*[:\-]?\s*([A-Z0-9][A-Z0-9\/\-]{3,})",
        };

        foreach (var p in patterns)
        {
            var m = Regex.Match(t, p, RegexOptions.IgnoreCase);
            if (m.Success)
            {
                var val = m.Groups[1].Value.Trim().TrimEnd('.', ',', ';');
                if (val.Length >= 4) return val;
            }
        }
        return null;
    }

    private static (DateTime inizio, DateTime fine, bool trovato) EstraiPeriodo(string t)
    {
        var patterns = new[]
        {
            $@"(?:periodo|consumi|competenza|fatturazione|riferimento)[\s\S]{{0,40}}?dal\s+{Data}\s+al\s+{Data}",
            $@"dal\s+{Data}\s+al\s+{Data}",
            $@"periodo[\s\S]{{0,20}}?{Data}\s*[-–]\s*{Data}",
        };

        foreach (var p in patterns)
        {
            var m = Regex.Match(t, p, RegexOptions.IgnoreCase);
            if (m.Success)
            {
                var d1 = ParseData(m.Groups[1].Value);
                var d2 = ParseData(m.Groups[2].Value);
                if (d1 is not null && d2 is not null && d2 >= d1)
                    return (d1.Value, d2.Value, true);
            }
        }

        // Fallback: nessun periodo riconosciuto → ultimo bimestre fino a oggi (dato non affidabile).
        var fine = DateTime.UtcNow.Date;
        return (fine.AddMonths(-2), fine, false);
    }

    private static decimal? EstraiConsumoTotaleKwh(string t)
    {
        var patterns = new[]
        {
            $@"(?:consumo\s+(?:totale|fatturato|rilevato|effettivo)|totale\s+consumo|energia\s+attiva\s+prelevata|totale\s+prelevato)[\s\S]{{0,30}}?{Num}\s*kwh",
            $@"{Num}\s*kwh\s+(?:totali|complessivi)",
        };

        foreach (var p in patterns)
        {
            var m = Regex.Match(t, p, RegexOptions.IgnoreCase);
            if (m.Success)
            {
                var v = ParseImporto(m.Groups[1].Value);
                if (v is > 0) return v;
            }
        }

        // Ultima spiaggia: prendi il valore in kWh più grande presente nel testo.
        decimal max = 0m;
        foreach (Match m in Regex.Matches(t, Num + @"\s*kwh", RegexOptions.IgnoreCase))
        {
            var v = ParseImporto(m.Groups[1].Value);
            if (v is not null && v > max) max = v.Value;
        }
        return max > 0 ? max : null;
    }

    private static decimal? EstraiKwhFascia(string t, string fascia)
    {
        // "F1 ... 164 kWh" oppure "Fascia 1 ... 164".
        var patterns = new[]
        {
            $@"\bF\s*{fascia}\b[\s\S]{{0,25}}?{Num}\s*kwh",
            $@"fascia\s*{fascia}\b[\s\S]{{0,25}}?{Num}\s*kwh",
            $@"\bF\s*{fascia}\b\s*[:\-]?\s*{Num}\b",
        };

        foreach (var p in patterns)
        {
            var m = Regex.Match(t, p, RegexOptions.IgnoreCase);
            if (m.Success)
            {
                var v = ParseImporto(m.Groups[1].Value);
                if (v is > 0) return v;
            }
        }
        return null;
    }

    private static decimal? EstraiConsumoSmc(string t)
    {
        var patterns = new[]
        {
            $@"(?:consumo[\s\S]{{0,30}}?)?{Num}\s*(?:smc|sm3|sm³)\b",
            $@"(?:consumo|volume)[\s\S]{{0,30}}?{Num}\s*(?:standard\s*m|metri\s*cubi)",
        };

        foreach (var p in patterns)
        {
            var m = Regex.Match(t, p, RegexOptions.IgnoreCase);
            if (m.Success)
            {
                var v = ParseImporto(m.Groups[1].Value);
                if (v is > 0) return v;
            }
        }
        return null;
    }

    private static List<VoceDiCostoDto> EstraiVociDiCosto(string t)
    {
        var voci = new List<VoceDiCostoDto>();

        void Aggiungi(CategoriaCosto cat, string descrizione, params string[] labels)
        {
            foreach (var label in labels)
            {
                var v = ImportoVicino(t, label);
                if (v is > 0)
                {
                    voci.Add(new VoceDiCostoDto(cat, descrizione, Math.Round(v.Value, 2)));
                    return;
                }
            }
        }

        Aggiungi(CategoriaCosto.MateriaEnergia, "Spesa per la materia energia",
            @"spesa\s+per\s+la\s+materia\s+energia", @"materia\s+energia",
            @"spesa\s+per\s+la\s+materia\s+gas(?:\s+naturale)?", @"materia\s+gas");

        Aggiungi(CategoriaCosto.TrasportoGestione, "Spesa per il trasporto e la gestione del contatore",
            @"spesa\s+per\s+il\s+trasporto\s+e\s+(?:la\s+)?gestione\s+del\s+contatore",
            @"trasporto\s+e\s+(?:la\s+)?gestione\s+del\s+contatore", @"trasporto\s+e\s+gestione");

        Aggiungi(CategoriaCosto.OneriDiSistema, "Spesa per oneri di sistema",
            @"spesa\s+per\s+(?:gli\s+)?oneri\s+di\s+sistema", @"oneri\s+di\s+sistema");

        Aggiungi(CategoriaCosto.Imposte, "Imposte e IVA",
            @"totale\s+imposte(?:\s+e\s+iva)?", @"imposte\s+e\s+iva", @"imposte", @"accise");

        return voci;
    }

    // ---------------------------------------------------------------------
    // Helper
    // ---------------------------------------------------------------------

    /// <summary>Trova il primo importo (€) che segue una certa etichetta entro una finestra di testo.</summary>
    private static decimal? ImportoVicino(string t, string labelPattern)
    {
        // L'etichetta NON deve contenere gruppi di cattura: l'unico gruppo è quello del numero.
        var pattern = $@"(?:{labelPattern})[\s\S]{{0,60}}?€?\s*{Num}\s*(?:€|euro)?";
        var m = Regex.Match(t, pattern, RegexOptions.IgnoreCase);
        return m.Success ? ParseImporto(m.Groups[1].Value) : null;
    }

    /// <summary>Converte un numero in formato italiano ("1.234,56") in decimal.</summary>
    private static decimal? ParseImporto(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var s = raw.Trim().Replace(" ", string.Empty).Replace(" ", string.Empty);

        if (s.Contains(','))
        {
            // Virgola = separatore decimale ⇒ i punti sono migliaia.
            s = s.Replace(".", string.Empty).Replace(',', '.');
        }
        else if (Regex.IsMatch(s, @"^\d{1,3}(\.\d{3})+$"))
        {
            // Solo punti come separatori di migliaia (es. "1.234").
            s = s.Replace(".", string.Empty);
        }
        // Altrimenti l'eventuale punto è già decimale (es. "342" o "342.5").

        return decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : null;
    }

    private static DateTime? ParseData(string raw)
    {
        var s = raw.Trim();
        if (DateTime.TryParseExact(s, FormatiData, It, DateTimeStyles.None, out var d)) return d;
        if (DateTime.TryParse(s, It, DateTimeStyles.None, out d)) return d;
        return null;
    }

    /// <summary>Normalizza il testo: uniforma spazi e a-capo, conserva la struttura per riga.</summary>
    private static string Normalizza(string testo)
    {
        var sb = new StringBuilder(testo.Length);
        foreach (var ch in testo)
        {
            if (ch == ' ') sb.Append(' ');           // no-break space
            else if (ch == '\t') sb.Append(' ');
            else sb.Append(ch);
        }
        // Comprimi spazi multipli ma mantieni gli a-capo.
        var s = Regex.Replace(sb.ToString(), @"[ ]{2,}", " ");
        return s;
    }

    private static decimal CalcolaConfidenza(
        bool importoTrovato, bool periodoTrovato, bool consumoTrovato,
        bool fasceTrovate, bool vociTrovate, bool numeroTrovato, decimal? confidenzaMotore)
    {
        // Pesi: l'importo e il consumo sono i campi più importanti.
        decimal punteggio = 0m, totale = 0m;
        void Voce(bool ok, decimal peso) { totale += peso; if (ok) punteggio += peso; }

        Voce(importoTrovato, 0.30m);
        Voce(consumoTrovato, 0.25m);
        Voce(periodoTrovato, 0.15m);
        Voce(vociTrovate, 0.15m);
        Voce(fasceTrovate, 0.10m);
        Voce(numeroTrovato, 0.05m);

        var confParsing = totale > 0 ? punteggio / totale : 0m;

        // Se il motore OCR fornisce una sua confidenza, la mediamo con quella di parsing.
        var finale = confidenzaMotore is not null
            ? (confParsing + confidenzaMotore.Value) / 2m
            : confParsing;

        return Math.Round(Math.Clamp(finale, 0m, 1m), 2);
    }
}
