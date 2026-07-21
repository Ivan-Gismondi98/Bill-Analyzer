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
public class LettureController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public LettureController(AppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<LetturaContatoreDto>>> Lista([FromQuery] Guid contrattoId)
    {
        var letture = await _db.Letture
            .Where(l => l.ContrattoId == contrattoId && l.Contratto!.UtenteId == _currentUser.UtenteId)
            .OrderByDescending(l => l.DataLettura)
            .ToListAsync();
        return Ok(letture.Select(l => l.ToDto()));
    }

    /// <summary>Restituisce l'ultima lettura registrata per il contratto (base per la previsione).</summary>
    [HttpGet("ultima")]
    public async Task<ActionResult<LetturaContatoreDto>> Ultima([FromQuery] Guid contrattoId)
    {
        var lettura = await _db.Letture
            .Where(l => l.ContrattoId == contrattoId && l.Contratto!.UtenteId == _currentUser.UtenteId)
            .OrderByDescending(l => l.DataLettura)
            .FirstOrDefaultAsync();
        return lettura is null ? NoContent() : Ok(lettura.ToDto());
    }

    [HttpPost]
    public async Task<ActionResult<LetturaContatoreDto>> Crea(CreateLetturaRequest req)
    {
        var contratto = await _db.Contratti
            .FirstOrDefaultAsync(c => c.Id == req.ContrattoId && c.UtenteId == _currentUser.UtenteId);
        if (contratto is null) return BadRequest(new { message = "Contratto non valido." });

        var lettura = new LetturaContatore
        {
            ContrattoId = req.ContrattoId,
            DataLettura = req.DataLettura,
            ValoreTotale = req.ValoreTotale,
            ValoreF1 = req.ValoreF1,
            ValoreF2 = req.ValoreF2,
            ValoreF3 = req.ValoreF3,
            Note = req.Note,
            DaBolletta = false
        };
        _db.Letture.Add(lettura);
        await _db.SaveChangesAsync();

        // Storico limitato alle ultime 3 auto-letture per contratto: le più vecchie vengono rimosse.
        await ManteniUltime3(req.ContrattoId);

        return Ok(lettura.ToDto());
    }

    private const int MaxStorico = 3;

    /// <summary>Conserva solo le <see cref="MaxStorico"/> auto-letture più recenti del contratto.</summary>
    private async Task ManteniUltime3(Guid contrattoId)
    {
        var daRimuovere = await _db.Letture
            .Where(l => l.ContrattoId == contrattoId)
            .OrderByDescending(l => l.DataLettura)
            .ThenByDescending(l => l.CreatedAt)
            .Skip(MaxStorico)
            .ToListAsync();

        if (daRimuovere.Count > 0)
        {
            _db.Letture.RemoveRange(daRimuovere);
            await _db.SaveChangesAsync();
        }
    }
}
