using BollettaAnalyzer.Application.Common.Exceptions;
using BollettaAnalyzer.Application.Common.Interfaces;
using BollettaAnalyzer.Application.DTOs;
using BollettaAnalyzer.Application.Mapping;
using BollettaAnalyzer.Domain.Entities;
using BollettaAnalyzer.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BollettaAnalyzer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BolletteController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IBillOcrService _ocr;
    private readonly ISuggerimentiService _suggerimenti;

    public BolletteController(AppDbContext db, ICurrentUser currentUser,
        IBillOcrService ocr, ISuggerimentiService suggerimenti)
    {
        _db = db;
        _currentUser = currentUser;
        _ocr = ocr;
        _suggerimenti = suggerimenti;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BollettaDto>>> Lista([FromQuery] Guid? contrattoId = null)
    {
        var query = _db.Bollette
            .Include(b => b.VociDiCosto)
            .Where(b => b.Contratto!.UtenteId == _currentUser.UtenteId);

        if (contrattoId is not null)
            query = query.Where(b => b.ContrattoId == contrattoId);

        var bollette = await query.OrderByDescending(b => b.PeriodoFine).ToListAsync();
        return Ok(bollette.Select(b => b.ToDto()));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AnalisiBollettaDto>> Analisi(Guid id)
    {
        var bolletta = await _db.Bollette
            .Include(b => b.VociDiCosto)
            .Include(b => b.Contratto)
            .FirstOrDefaultAsync(b => b.Id == id && b.Contratto!.UtenteId == _currentUser.UtenteId);

        if (bolletta is null) return NotFound();
        return Ok(CostruisciAnalisi(bolletta));
    }

    /// <summary>
    /// Carica un PDF/immagine di bolletta, la elabora tramite OCR (mock) e la salva
    /// collegandola al contratto indicato. Restituisce l'analisi completa.
    /// </summary>
    [HttpPost("upload")]
    [RequestSizeLimit(20_000_000)]
    public async Task<ActionResult<AnalisiBollettaDto>> Upload(
        [FromForm] Guid contrattoId, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "Nessun file caricato." });

        var contratto = await _db.Contratti
            .FirstOrDefaultAsync(c => c.Id == contrattoId && c.UtenteId == _currentUser.UtenteId, ct);
        if (contratto is null) return BadRequest(new { message = "Contratto non valido." });

        OcrResultDto ocr;
        try
        {
            await using var stream = file.OpenReadStream();
            ocr = await _ocr.EstraiDatiAsync(stream, file.FileName, ct);
        }
        catch (OcrParsingException ex)
        {
            // Documento illeggibile/non analizzabile: errore "di dominio", non un 500.
            return UnprocessableEntity(new { message = ex.Message });
        }

        // Conservazione "leggera": teniamo solo i dati estratti dell'ULTIMA bolletta
        // caricata per il contratto (fino al prossimo upload). Il file originale non
        // viene mai salvato, così non appesantiamo server e memoria.
        var precedenti = await _db.Bollette.Where(b => b.ContrattoId == contratto.Id).ToListAsync(ct);
        if (precedenti.Count > 0) _db.Bollette.RemoveRange(precedenti);

        var bolletta = new Bolletta
        {
            ContrattoId = contratto.Id,
            Contratto = contratto,
            NumeroFattura = ocr.NumeroFattura,
            PeriodoInizio = ocr.PeriodoInizio,
            PeriodoFine = ocr.PeriodoFine,
            DataEmissione = DateTime.UtcNow,
            ImportoTotale = ocr.ImportoTotale,
            ConsumoTotaleKwh = ocr.ConsumoTotaleKwh,
            ConsumoF1Kwh = ocr.ConsumoF1Kwh,
            ConsumoF2Kwh = ocr.ConsumoF2Kwh,
            ConsumoF3Kwh = ocr.ConsumoF3Kwh,
            ConsumoSm3 = ocr.ConsumoSm3,
            FileOriginale = file.FileName,
            DaOcr = true,
            VociDiCosto = ocr.VociDiCosto
                .Select(v => new VoceDiCosto { Categoria = v.Categoria, Descrizione = v.Descrizione, Importo = v.Importo })
                .ToList()
        };

        _db.Bollette.Add(bolletta);
        await _db.SaveChangesAsync(ct);

        return Ok(CostruisciAnalisi(bolletta));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Elimina(Guid id)
    {
        var b = await _db.Bollette.FirstOrDefaultAsync(x => x.Id == id && x.Contratto!.UtenteId == _currentUser.UtenteId);
        if (b is null) return NotFound();
        _db.Bollette.Remove(b);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private AnalisiBollettaDto CostruisciAnalisi(Bolletta b)
    {
        var totaleFasce = b.ConsumoF1Kwh + b.ConsumoF2Kwh + b.ConsumoF3Kwh;
        var ripartizione = totaleFasce > 0
            ? new RipartizioneFasceDto(
                Math.Round(b.ConsumoF1Kwh / totaleFasce * 100, 1),
                Math.Round(b.ConsumoF2Kwh / totaleFasce * 100, 1),
                Math.Round(b.ConsumoF3Kwh / totaleFasce * 100, 1))
            : new RipartizioneFasceDto(0, 0, 0);

        var suggerimenti = b.Contratto is not null
            ? _suggerimenti.GeneraSuggerimenti(b.Contratto, b)
            : new List<SuggerimentoDto>();

        return new AnalisiBollettaDto(
            b.ToDto(),
            b.VociDiCosto.Select(v => v.ToDto()).ToList(),
            ripartizione,
            suggerimenti);
    }
}
