using BollettaAnalyzer.Application.DTOs;
using BollettaAnalyzer.Domain.Entities;

namespace BollettaAnalyzer.Application.Mapping;

/// <summary>Estensioni di mapping da entità di dominio a DTO.</summary>
public static class MappingExtensions
{
    public static UtenteDto ToDto(this Utente u) =>
        new(u.Id, u.Email, u.Nome, u.Cognome, u.Telefono, u.Indirizzo, u.Citta, u.Cap);

    public static ContrattoDto ToDto(this Contratto c) =>
        new(c.Id, c.TipoFornitura, c.Fornitore, c.CodicePod, c.CodicePdr, c.NomeOfferta,
            c.TipoTariffa, c.PotenzaImpegnataKw, c.PrezzoKwhMonorario, c.PrezzoKwhF1, c.PrezzoKwhF2,
            c.PrezzoKwhF3, c.PrezzoSm3, c.QuotaFissaMensile, c.DataAttivazione, c.Attivo);

    public static VoceDiCostoDto ToDto(this VoceDiCosto v) =>
        new(v.Categoria, v.Descrizione, v.Importo);

    public static BollettaDto ToDto(this Bolletta b) =>
        new(b.Id, b.ContrattoId, b.NumeroFattura, b.PeriodoInizio, b.PeriodoFine, b.DataEmissione,
            b.ImportoTotale, b.ConsumoTotaleKwh, b.ConsumoF1Kwh, b.ConsumoF2Kwh, b.ConsumoF3Kwh,
            b.ConsumoSm3, b.DaOcr, b.ConfidenzaOcr, b.VociDiCosto.Select(v => v.ToDto()).ToList());

    public static LetturaContatoreDto ToDto(this LetturaContatore l) =>
        new(l.Id, l.ContrattoId, l.DataLettura, l.ValoreTotale, l.ValoreF1, l.ValoreF2, l.ValoreF3, l.DaBolletta, l.Note);

    public static DispositivoDto ToDto(this DispositivoElettronico d) =>
        new(d.Id, d.Nome, d.PotenzaWatt, d.OreUtilizzoGiornaliere, d.GiorniSettimana,
            d.Fasce, d.ConsumoGiornalieroKwh, d.ConsumoMensileKwh);
}
